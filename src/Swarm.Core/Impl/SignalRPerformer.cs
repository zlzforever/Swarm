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
                for (int i = 0; i < clients.Count; ++i)
                {
                    var client = clients[i];

                    await store.AddJobState(jobContext.JobId, jobContext.TraceId, client.Name, State.Performing,
                        $"Performing on client: [{client.Name}, {client.Group}, {client.Ip}]."
                    );

                    var shardingParameter = i < shardingParameters.Length ? shardingParameters[i] : "";

                    jobContext.CurrentSharding = i;
                    jobContext.CurrentShardingParameter = shardingParameter;

                    if (jobContext.Parameters.ContainsKey(SwarmConts.ArgumentsProperty))
                    {
                        var arguments = jobContext.Parameters[SwarmConts.ArgumentsProperty];
                        if (!string.IsNullOrWhiteSpace(arguments))
                        {
                            arguments = arguments.Replace("%JobId%", jobContext.JobId);
                            arguments = arguments.Replace("%TraceId%", jobContext.TraceId);
                            arguments = arguments.Replace("%Sharding%", jobContext.Sharding.ToString());
                            arguments = arguments.Replace("%Partition%", i.ToString());
                            arguments = arguments.Replace("%ShardingParameter%", shardingParameter);
                            jobContext.Parameters[SwarmConts.ArgumentsProperty] = arguments;
                        }
                    }

                    await hubContext.Clients.Client(client.ConnectionId).SendAsync("Trigger", jobContext);
                    await store.ChangeJobState(jobContext.TraceId, client.Name, State.Performed,
                        $"Performed on client: [{client.Name}, {client.Group}, {client.Ip}]."
                    );
                }
            }
            else
            {
                logger.LogError($"Unfounded available client for job {jobContext.JobId}.");
                await store.AddJobState(jobContext.JobId, jobContext.TraceId, "Internal", State.Performing,
                    "Unfounded available client");
            }
        }
    }
}