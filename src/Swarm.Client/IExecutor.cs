using System;
using System.Threading.Tasks;
using Swarm.Basic;

namespace Swarm.Client
{
    public interface IExecutor
    {
        Task<int> Execute(JobContext context, Action<string, string, string> logger);
    }
}