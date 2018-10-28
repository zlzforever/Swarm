using System.Collections.Generic;

namespace Swarm.Core
{
    public class SwarmOptions
    {
        public string ConnectionString { get; set; }
        public HashSet<string> AccessTokens { get; set; }
    }
}