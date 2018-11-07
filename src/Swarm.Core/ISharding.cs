using System.Threading.Tasks;
using Swarm.Basic.Entity;

namespace Swarm.Core
{
    /// <summary>
    /// Quartz 分片接口
    /// </summary>
    public interface ISharding
    {
        Task<Node> GetShardingNode();
        Task AdjustLoad();
        Task DisconnectNode(string schedName, string schedInstanceId);
        Task RegisterNode(Node node);
        Task<Node> GetNode(string schedName, string schedInstanceId);
    }
}