using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;

namespace Swarm.Core.Impl
{
    /// <summary>
    /// 默认分片实现, 基于数据库
    /// </summary>
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

        public async Task DisconnectNode(string schedName, string schedInstanceId)
        {
            await _store.DisconnectNode(schedName, schedInstanceId);
        }

        public async Task RegisterNode(Node node)
        {
            await _store.RegisterNode(node);
        }

        public async Task<Node> GetNode(string schedName, string schedInstanceId)
        {
            return await _store.GetNode(schedName, schedInstanceId);
        }
    }
}