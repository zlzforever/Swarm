using System.Threading;
using System.Threading.Tasks;

namespace Swarm.Client
{
    public interface ISwarmClient
    {
        Task Start(CancellationToken cancellationToken = default);
        void Stop();
    }
}