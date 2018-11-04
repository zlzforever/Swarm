using System.Threading.Tasks;
using Swarm.Basic.Entity;

namespace Swarm.Core
{
    public interface ISharding
    {
        Task<Node> GetShardingNode();
    }
}