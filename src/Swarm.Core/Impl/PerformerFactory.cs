using System.Collections.Generic;
using Swarm.Basic;

namespace Swarm.Core.Impl
{
    public static class PerformerFactory
    {
        public static readonly Dictionary<Performer, IPerformer> Performers = new Dictionary<Performer, IPerformer>();

        static PerformerFactory()
        {
            Performers.Add(Performer.SignalR, new SignalRPerformer());
        }

        public static IPerformer Create(Performer performer)
        {
            if (!Performers.ContainsKey(performer))
            {
                throw new SwarmException($"Unsupported trigger: {performer}.");
            }

            return Performers[performer];
        }
    }
}