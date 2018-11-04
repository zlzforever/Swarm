using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Quartz;
using Swarm.Basic;
using Swarm.Basic.Entity;
using Swarm.Core.Common.Internal;

namespace Swarm.Core.Impl
{
    public class TriggerJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var store = Ioc.GetRequiredService<ISwarmStore>();
            var logger = Ioc.GetRequiredService<ILoggerFactory>().CreateLogger<TriggerJob>();
            await Ioc.GetRequiredService<ISwarmCluster>().IncreaseTriggerTime();
            var jobId = context.JobDetail.Key.Name;
            try
            {
                Job job = await store.GetJob(jobId);
                if (job == null)
                {
                    logger.LogWarning($"Job {jobId} is missing");
                    return;
                }

                var traceId = Guid.NewGuid().ToString("N");
                var performer = PerformerFactory.Create(job.Performer);
                var jobContext = new JobContext
                {
                    FireTimeUtc = context.FireTimeUtc,
                    NextFireTimeUtc = context.NextFireTimeUtc,
                    Sharding = job.Sharding,
                    CurrentSharding = 0,
                    CurrentShardingParameter = "",
                    ShardingParameters = job.ShardingParameters,
                    ScheduledFireTimeUtc = context.ScheduledFireTimeUtc,
                    PreviousFireTimeUtc = context.PreviousFireTimeUtc,
                    Parameters = new Dictionary<string, string>(),
                    Name = job.Name,
                    Executor = job.Executor,
                    Group = job.Group,
                    JobId = job.Id,
                    TraceId = traceId,
                    AllowConcurrent = job.AllowConcurrent
                };
                foreach (var jobProperty in job.Properties)
                {
                    jobContext.Parameters.Add(jobProperty.Key, jobProperty.Value);
                }

                await performer.Perform(jobContext);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Perform job {jobId} failed: {ex.Message}");
            }
        }
    }
}