using System.Threading.Tasks;
using Swarm.Basic;

namespace Swarm.Core
{
    public interface IPerformer
    {
        Task Perform(JobContext jobContext);
    }
}