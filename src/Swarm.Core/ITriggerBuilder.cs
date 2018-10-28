using System.Collections.Generic;
using Quartz;

namespace Swarm.Core
{
    public interface ITriggerBuilder
    {
        ITrigger Build(string id, Dictionary<string, string> properties);
    }
}