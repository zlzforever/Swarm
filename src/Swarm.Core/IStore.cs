using System.Collections.Generic;
using System.Threading.Tasks;
using Swarm.Basic;
using Swarm.Basic.Entity;

namespace Swarm.Core
{
    public interface IStore
    {
        Task<bool> RegisterClient(Client client);
        Task RemoveClient(string connectionId);
        Task<IEnumerable<Client>> GetClients(string group);
        void CleanClients();
        Task<bool> CheckJobExists(string jobId);
        Task<bool> CheckJobExists(string name, string group);
        Task<string> AddJob(Job job, IDictionary<string, string> properties);
        Task UpdateJob(Job job, IDictionary<string, string> properties);
        Task DeleteJob(string jobId);
        Task<Job> GetJob(string jobId);
        Task<List<JobProperty>> GetJobProperties(string jobId);
        Task ChangeJobState(string jobId, State state);
        Task AddJobState(string jobId, string traceId, string client, State state, string msg);
        Task ChangeJobState(string traceId, string client, State state, string msg);
        Task AddLog(string jobId, string traceId, string msg);
        Task<List<JobState>> GetCurrentJobStates(string jobId);
    }
}