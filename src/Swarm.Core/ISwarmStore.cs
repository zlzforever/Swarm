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
        Task<IEnumerable<Client>> GetAvailableClients(string group);
        Task DisconnectAllClients();
        Task ClientHeartbeat(string name, string group);

        #endregion

        Task<Job> GetJob(string name, string group);
        Task<string> AddJob(Job job);
        Task UpdateJob(Job job);
        Task DeleteJob(string jobId);
        Task<Job> GetJob(string jobId);
        //Task ChangeJobState(string jobId, State state);

        Task AddJobState(JobState jobState);
        Task<JobState> GetJobState(string traceId, string client, int sharding);
        Task UpdateJobState(JobState jobState);

        Task RegisterNode(Node node);
        Task<Node> GetMinimumTriggerTimesNode();
        Task<Node> GetNode(string nodeId);
        Task IncreaseTriggerTime(string name, string nodeId);
    }
}