using System.Collections.Generic;
using Quartz;
using Swarm.Basic;

namespace Swarm.Core.Impl
{
    public static class TriggerFactory
    {
        public static readonly Dictionary<Trigger, ITriggerBuilder> TriggerBuilders =
            new Dictionary<Trigger, ITriggerBuilder>();

        static TriggerFactory()
        {
            TriggerBuilders.Add(Trigger.Cron, new CronTriggerBuilder());
        }

        public static ITrigger Create(Trigger trigger, string id, Dictionary<string, string> parameters)
        {
            if (!TriggerBuilders.ContainsKey(trigger))
            {
                throw new SwarmException($"Unsupported trigger: {trigger}.");
            }

            return TriggerBuilders[trigger].Build(id, parameters);
        }
    }
}