using System.Threading.Tasks;
using Swarm.Basic.Entity;

namespace Swarm.Core
{
    public interface ILogStore
    {
        Task AddLog(Log log);
    }
}