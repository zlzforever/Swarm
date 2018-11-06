using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Swarm.Basic;
using Swarm.Basic.Common;
using Swarm.Basic.Entity;
using Swarm.Core.Common.Internal;
using Swarm.Core.SignalR;

namespace Swarm.Core.Impl
{
    public class SignalRPerformer : IPerformer
    {
        private readonly int _retryTimes = 5;

        public async Task<bool> Perform(JobContext jobContext)
        {
            var store = Ioc.GetRequiredService<ISwarmStore>();
            var logger = Ioc.GetRequiredService<ILoggerFactory>().CreateLogger<SignalRPerformer>();
            var hubContext = Ioc.GetRequiredService<IHubContext<ClientHub>>();
            var options = Ioc.GetRequiredService<IOptions<SwarmOptions>>().Value;

            var clients = (await store.GetAvailableClients(jobContext.Group)).ToList();
            if (clients.Any())
            {
                // TODO: 实现分片算法
                var shardingParameters = jobContext.ShardingParameters == null
                    ? new string[0]
                    : jobContext.ShardingParameters?.Split(new[] {";"}, StringSplitOptions.RemoveEmptyEntries);

                var dic = new Dictionary<int, int>();
                for (int s = 0; s < jobContext.Sharding; ++s)
                {
                    dic.Add(s, 0);
                }

                for (int i = 0, j = 0; j < clients.Count && i < jobContext.Sharding;)
                {
                    if (dic[i] >= _retryTimes)
                    {
                        return false;
                    }

                    var client = clients[j];

                    var shardingParameter = i < shardingParameters.Length ? shardingParameters[i] : "";

                    var jc = jobContext.Clone();

                    try
                    {
                        jc.CurrentSharding = i + 1;
                        jc.CurrentShardingParameter = shardingParameter;

                        if (jc.Parameters.ContainsKey(SwarmConsts.ArgumentsProperty))
                        {
                            var arguments = jc.Parameters[SwarmConsts.ArgumentsProperty];
                            if (!string.IsNullOrWhiteSpace(arguments))
                            {
                                arguments = arguments.Replace("%jobid%", jc.JobId);
                                arguments = arguments.Replace("%traceid%", jc.TraceId);
                                arguments = arguments.Replace("%sharding%", jc.Sharding.ToString());
                                arguments = arguments.Replace("%currentsharding%", jc.CurrentSharding.ToString());
                                arguments = arguments.Replace("%currentshardingparameter%",
                                    jc.CurrentShardingParameter);
                                arguments = arguments.Replace("%name%", jc.Name);
                                arguments = arguments.Replace("%group%", jc.Group);
                                arguments = arguments.Replace("%firetime%",
                                    jc.FireTimeUtc.ToString("yyyy-MM-dd hh:mm:ss"));

                                jc.Parameters[SwarmConsts.ArgumentsProperty] = arguments;
                            }
                        }

                        await hubContext.Clients.Client(client.ConnectionId).SendAsync("Trigger", jc);

                        await store.AddOrUpdateClientProcess(new ClientProcess
                            {
                                Name = client.Name,
                                Group = client.Group,
                                JobId = jc.JobId,
                                TraceId = jc.TraceId,
                                Sharding = jc.CurrentSharding,
                                State = State.Performed,
                                Msg = $"Performed on {client}",
                                App = jc.Parameters.GetValue(SwarmConsts.ApplicationProperty),
                                AppArguments = jc.Parameters.GetValue(SwarmConsts.ArgumentsProperty),
                                ProcessId = 0
                            }
                        );

                        logger.LogInformation($"Perform {jobContext.JobId} on {client} success");
                        i++;
                        j++;
                        if (j == clients.Count)
                        {
                            j = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Perform {jobContext.JobId} on {client} failed: {ex.Message}");
                        dic[i]++;
                        // 分片不增加, 尝试其它节点
                        j++;
                        if (j == clients.Count)
                        {
                            j = 0;
                        }
                    }
                }

                return true;
            }
            else
            {
                logger.LogError($"Unfounded available client for job {jobContext.JobId}");
                await store.AddOrUpdateClientProcess(new ClientProcess
                    {
                        Name = options.SchedInstanceId,
                        Group = options.SchedName,
                        JobId = jobContext.JobId,
                        TraceId = jobContext.TraceId,
                        Sharding = 1,
                        State = State.Exit,
                        Msg = "Unfounded available client",
                        App = jobContext.Parameters.GetValue(SwarmConsts.ApplicationProperty),
                        AppArguments = jobContext.Parameters.GetValue(SwarmConsts.ArgumentsProperty),
                        ProcessId = 0
                    }
                );
                return false;
            }
        }
    }
}