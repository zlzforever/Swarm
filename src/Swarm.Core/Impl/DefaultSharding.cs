using System.Threading.Tasks;
using Swarm.Basic.Entity;

namespace Swarm.Core.Impl
{
    public class DefaultSharding : ISharding
    {
        private readonly ISwarmStore _store;

        public DefaultSharding(ISwarmStore store)
        {
            _store = store;
        }

        public async Task<Node> GetShardingNode()
        {
           return await _store.GetMinimumTriggerTimesNode();
        }
    }
}