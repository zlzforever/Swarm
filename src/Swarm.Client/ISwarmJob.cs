using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.Basic;

namespace Swarm.Client
{
    public interface ISwarmJob
    {
        Task Handle(JobContext context);
    }
}