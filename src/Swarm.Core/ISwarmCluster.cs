using System.Threading;
using System.Threading.Tasks;

namespace Swarm.Core
{
    public interface ISwarmCluster
    {
        Task Start(CancellationToken token = default);

        Task IncreaseTriggerTime();
    }
}