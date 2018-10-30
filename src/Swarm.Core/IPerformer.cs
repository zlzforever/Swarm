using System.Threading.Tasks;
using Swarm.Basic;

namespace Swarm.Core
{
    public interface IPerformer
    {
        Task<bool> Perform(JobContext jobContext);
    }
}