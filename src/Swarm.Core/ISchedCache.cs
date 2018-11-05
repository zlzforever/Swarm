using Quartz;

namespace Swarm.Core
{
    public interface ISchedCache
    {
        IScheduler GetOrCreate(string cluster, string nodeId, string provider, string connectionString);
    }
}