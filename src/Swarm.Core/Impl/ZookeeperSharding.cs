using System.Threading.Tasks;
using Swarm.Basic.Entity;

namespace Swarm.Core.Impl
{
    /// <summary>
    /// TODO: 基于 Zookeeper 实现的分片
    /// </summary>
    public class ZookeeperSharding : ISharding
    {
        public Task<Node> GetShardingNode()
        {
            throw new System.NotImplementedException();
        }

        public Task AdjustLoad()
        {
            throw new System.NotImplementedException();
        }

        public Task DisconnectNode(string schedName, string schedInstanceId)
        {
            throw new System.NotImplementedException();
        }

        public Task RegisterNode(Node node)
        {
            throw new System.NotImplementedException();
        }

        public Task<Node> GetNode(string schedName, string schedInstanceId)
        {
            throw new System.NotImplementedException();
        }
    }
}