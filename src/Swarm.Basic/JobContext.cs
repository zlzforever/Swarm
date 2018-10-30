using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Swarm.Core")]

namespace Swarm.Basic
{
    public class JobContext
    {
        public string JobId { get; set; }

        public string TraceId { get; set; }

        public string Name { get; set; }

        public string Group { get; set; }

        public Executor Executor { get; set; }

        public DateTimeOffset FireTimeUtc { get; set; }

        public DateTimeOffset? ScheduledFireTimeUtc { get; set; }

        public DateTimeOffset? PreviousFireTimeUtc { get; set; }

        public DateTimeOffset? NextFireTimeUtc { get; set; }

        public int Sharding { get; set; } = 1;

        public string ShardingParameters { get; set; }

        public int CurrentSharding { get; set; }

        public string CurrentShardingParameter { get; set; }

        public bool ConcurrentExecutionDisallowed { get; set; }

        public Dictionary<string, string> Parameters { get; set; }

        public JobContext Clone()
        {
            var jc = (JobContext) MemberwiseClone();
            var dic = new Dictionary<string, string>();
            foreach (var kv in Parameters)
            {
                dic.Add(kv.Key, kv.Value);
            }

            jc.Parameters = dic;
            return jc;
        }
    }
}