using System.Threading.Tasks;
using Swarm.Basic.Entity;
using Swarm.Core.Common;

namespace Swarm.Core
{
    public interface INodeService
    {
        Task<ApiResult> GetNodes();
        Task<ApiResult> GetNodeStatistics();
    }
}