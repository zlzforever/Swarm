using System.Threading;
using System.Threading.Tasks;

namespace Swarm.Core
{
    public interface ISwarmCluster
    {
        Task Start(CancellationToken cancellationToken = default);
        Task Shutdown();
    }
}