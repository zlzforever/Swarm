using System.Threading.Tasks;
using Swarm.Basic;

namespace Swarm.Client
{
    public interface ISwarmJob
    {
        Task Handle(JobContext context);
    }
}