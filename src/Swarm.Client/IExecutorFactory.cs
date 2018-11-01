using Swarm.Basic;

namespace Swarm.Client
{
    public interface IExecutorFactory
    {
        IExecutor Create(Executor executor);
    }
}