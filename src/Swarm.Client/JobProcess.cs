using System;

namespace Swarm.Client
{
    public class JobProcess
    {
        public string JobId { get; set; }
        public string TraceId { get; set; }
        public int Sharding { get; set; }
        public int ProcessId { get; set; }
        public string Application { get; set; }
        public string Arguments { get; set; }
        public DateTimeOffset StartAt { get; set; }
    }
}