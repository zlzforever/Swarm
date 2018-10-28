using System;
using Quartz;
using Swarm.Basic.Entity;
using Swarm.Core.Impl;

namespace Swarm.Core.Common
{
    public static class JobExtensions
    {
        public static IJobDetail ToQuartzJob(this Job job)
        {
            job.Id = string.IsNullOrWhiteSpace(job.Id) ? Guid.NewGuid().ToString("N") : job.Id;
            return JobBuilder.Create<TriggerJob>().WithIdentity(job.Id).WithDescription(job.Description)
                .Build();
        }
    }
}