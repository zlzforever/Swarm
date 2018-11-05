using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;

namespace Swarm.Core.Impl
{
    public class DefaultSharding : ISharding
    {
        private readonly ISwarmStore _store;
        private readonly SwarmOptions _options;
        
        public DefaultSharding(ISwarmStore store, IOptions<SwarmOptions> options)
        {
            _store = store;
            _options = options.Value;
        }

        public async Task<Node> GetShardingNode()
        {
           return await _store.GetMinimumTriggerTimesNode();
        }

        public async Task AdjustLoad()
        {
            await _store.IncreaseTriggerTime(_options.SchedName, _options.SchedInstanceId);
        }
    }
}