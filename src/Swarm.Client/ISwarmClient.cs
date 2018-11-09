using System;
using System.Threading;
using System.Threading.Tasks;

namespace Swarm.Client
{
    public interface ISwarmClient
    {
        Task Run(CancellationToken cancellationToken);
        bool IsRunning { get; }
        event Action Closed;
    }
}