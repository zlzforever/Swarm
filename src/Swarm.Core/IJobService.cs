using System.Threading.Tasks;
using Swarm.Basic.Entity;
using Swarm.Core.Common;

namespace Swarm.Core
{
    public interface IJobService
    {
        Task<ApiResult> Create(Job job);
        Task<ApiResult> Delete(string jobId);
        Task<ApiResult> Exit(string jobId);
        Task<ApiResult> Get(string jobId);
    }
}