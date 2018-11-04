using Quartz;
using Quartz.Spi;

namespace Swarm.Core
{
    public interface ISchedulerCache
    {
        IScheduler Create(string cluster, string nodeId, string provider, string connectionString);
    }
}