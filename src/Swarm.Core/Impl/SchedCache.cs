using System.Collections.Concurrent;
using System.Collections.Specialized;
using Quartz;
using Quartz.Impl;

namespace Swarm.Core.Impl
{
    public class SchedCache : ISchedCache
    {
        private static readonly ConcurrentDictionary<string, IScheduler> Scheds =
            new ConcurrentDictionary<string, IScheduler>();

        public IScheduler GetOrCreate(string name, string nodeId, string provider, string connectionString)
        {
            var key = $"{name}_{nodeId}_{provider}_{connectionString}";
            if (Scheds.ContainsKey(key))
            {
                return Scheds[key];
            }

            var sched = new StdSchedulerFactory(new NameValueCollection
            {
                {"quartz.scheduler.instanceName", name},
                {"quartz.scheduler.instanceId", nodeId},
                {"quartz.jobStore.Clustered", "true"},
                {"quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz"},
                {"quartz.jobStore.driverDelegateType", $"Quartz.Impl.AdoJobStore.{provider}Delegate, Quartz"},
                {"quartz.jobStore.tablePrefix", "QRTZ_"},
                {"quartz.jobStore.useProperties", "true"},
                {"quartz.serializer.type", "json"},
                {"quartz.jobStore.dataSource", "swarn"},
                {"quartz.dataSource.swarn.provider", provider},
                {
                    "quartz.dataSource.swarn.connectionString", connectionString
                }
            }).GetScheduler().Result;

            while (!Scheds.TryAdd(key, sched))
            {
            }

            return Scheds[key];
        }
    }
}