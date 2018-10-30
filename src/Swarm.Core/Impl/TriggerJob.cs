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
            var jobId = context.JobDetail.Key.Name;
            try
            {
                Job job = await store.GetJob(jobId);
                if (job == null)
                {
                    logger.LogWarning($"Job {jobId} is missing.");
                    return;
                }

                var policy = Policy.Handle<Exception>().Retry(job.RetryCount <= 0 ? 1 : job.RetryCount,
                    (ex, count) => { logger.LogError(ex, $"Perform {jobId} failed [{count}]: {ex.Message}."); });

                await policy.Execute(async () =>
                {
                    var traceId = Guid.NewGuid().ToString("N");
                    var performer = PerformerFactory.Create(job.Performer);
                    await store.ChangeJobState(jobId, State.Performing);
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
                        ConcurrentExecutionDisallowed = job.ConcurrentExecutionDisallowed
                    };
                    foreach (var jobProperty in job.Properties)
                    {
                        jobContext.Parameters.Add(jobProperty.Key, jobProperty.Value);
                    }

                    var success = await performer.Perform(jobContext);
                    if (success)
                    {
                        await store.ChangeJobState(jobId, State.Performed);
                    }
                    else
                    {
                        await store.ChangeJobState(jobId, State.Exit);
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Perform job {jobId} failed: {ex.Message}.");
                await store.ChangeJobState(jobId, State.Exit);
            }
        }
    }
}