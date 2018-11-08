using System;
using Swarm.Basic;

namespace Swarm.Server.Models.Dto
{
    public class ClientProcessDto
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string TraceId { get; set; }
        public int Sharding { get; set; }
        public int ProcessId { get; set; }
        public State State { get; set; }
        public string App { get; set; }
        public string Arguments { get; set; }
        public string Msg { get; set; }
        public DateTimeOffset LastModificationTime { get; set; }
    }
}