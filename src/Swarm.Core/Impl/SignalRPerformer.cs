using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Swarm.Basic;
using Swarm.Core.Common.Internal;
using Swarm.Core.SignalR;

namespace Swarm.Core.Impl
{
    public class SignalRPerformer : IPerformer
    {
        public async Task Perform(JobContext jobContext)
        {
            var store = Ioc.GetRequiredService<IStore>();
            var logger = Ioc.GetRequiredService<ILoggerFactory>().CreateLogger<SignalRPerformer>();
            var hubContext = Ioc.GetRequiredService<IHubContext<ClientHub>>();

            var clients = (await store.GetClients(jobContext.Group)).ToList();
            if (clients.Any())
            {
                // TODO: 实现分片算法
                var shardingParameters = jobContext.ShardingParameters == null
                    ? new string[0]
                    : jobContext.ShardingParameters?.Split(new[] {";"}, StringSplitOptions.RemoveEmptyEntries);


                for (int i = 0, j = 0; j < clients.Count && i < jobContext.Sharding;)
                {
                    var client = clients[j];

                    var shardingParameter = i < shardingParameters.Length ? shardingParameters[i] : "";

                    var jc = jobContext.Clone();
                    jc.CurrentSharding = i;
                    jc.CurrentShardingParameter = shardingParameter;

                    await store.AddJobState(jc.JobId, jc.TraceId, client.Name,
                        jc.CurrentSharding, State.Performing,
                        $"Performing on client: [{client.Name}, {client.Group}, {client.Ip}]."
                    );

                    if (jc.Parameters.ContainsKey(SwarmConts.ArgumentsProperty))
                    {
                        var arguments = jc.Parameters[SwarmConts.ArgumentsProperty];
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
                            
                            jc.Parameters[SwarmConts.ArgumentsProperty] = arguments;
                        }
                    }

                    await hubContext.Clients.Client(client.ConnectionId).SendAsync("Trigger", jc);
                    await store.ChangeJobState(jc.TraceId, client.Name, jc.CurrentSharding,
                        State.Performed,
                        $"Performed on client: [{client.Name}, {client.Group}, {client.Ip}]."
                    );

                    i++;
                    j++;
                    if (j == clients.Count)
                    {
                        j = 0;
                    }
                }
            }
            else
            {
                logger.LogError($"Unfounded available client for job {jobContext.JobId}.");
                await store.AddJobState(jobContext.JobId, jobContext.TraceId, "Swarm", jobContext.CurrentSharding,
                    State.Performing,
                    "Unfounded available client");
            }
        }
    }
}