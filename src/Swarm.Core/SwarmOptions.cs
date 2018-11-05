using System.Collections.Generic;

namespace Swarm.Core
{
    public class SwarmOptions
    {
        public string ConnectionString { get; set; }
        public HashSet<string> AccessTokens { get; set; }
        public string SchedName { get; set; }
        public string SchedInstanceId { get; set; }
        public string Provider { get; set; }
        public string QuartzConnectionString { get; set; }
    }
}