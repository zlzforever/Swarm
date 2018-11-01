using System.Collections.Generic;
using System.Threading.Tasks;

namespace Swarm.Client
{
    public interface IProcessStore
    {
        void Add(JobProcess jobProcess);
        void Remove(ProcessKey key);
        bool Exists(ProcessKey key);
        int Count(string jobId);
        List<JobProcess> GetProcesses(string jobId);
        List<JobProcess> GetProcesses();
        JobProcess GetProcess(ProcessKey key);
    }
}