using System.Collections.Generic;

namespace Swarm.Core
{
    public class SwarmOptions
    {
        public string ConnectionString { get; set; }
        public HashSet<string> AccessTokens { get; set; }
        public string Name { get; set; }
        public string NodeId { get; set; }
        public string Provider { get; set; }
        public string QuartzConnectionString { get; set; }
    }
}