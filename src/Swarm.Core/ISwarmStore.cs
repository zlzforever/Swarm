using System.Collections.Generic;
using System.Threading.Tasks;
using Swarm.Basic;
using Swarm.Basic.Entity;

namespace Swarm.Core
{
    public interface ISwarmStore
    {
        #region Client

        Task<bool> AddClient(Client client);
        Task RemoveClient(int clientId);
        Task ConnectClient(string name, string group, string connectionId);
        Task<Client> GetClient(string name, string group);
        Task DisconnectClient(string name, string group);
        Task<IEnumerable<Client>> GetClients(string group);
        Task DisconnectAllClients();
        
        #endregion

        Task<bool> CheckJobExists(string jobId);
        Task<bool> IsJobExists(string name, string group);
        Task<string> AddJob(Job job);
        Task UpdateJob(Job job);
        Task DeleteJob(string jobId);
        Task<Job> GetJob(string jobId);
        Task<bool> IsJobExited(string jobId);
        Task ChangeJobState(string jobId, State state);

        Task AddJobState(JobState jobState);
        Task<JobState> GetJobState(string traceId, string client, int sharding);
        Task UpdateJobState(JobState jobState);
        
        Task AddLog(Log log);
        
    }
}