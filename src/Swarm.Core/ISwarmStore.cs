using System.Collections.Generic;
using System.Threading.Tasks;
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

        #region Job

        Task<Job> GetJob(string name, string group);
        Task<string> AddJob(Job job);
        Task UpdateJob(Job job);
        Task DeleteJob(string jobId);

        Task<Job> GetJob(string jobId);

        #endregion

        #region ClientProcess

        Task AddOrUpdateClientProcess(ClientProcess clientProcess);

        #endregion

        #region Node

        Task DisconnectNode(string schedName, string schedInstanceId);
        Task RegisterNode(Node node);
        Task<Node> GetMinimumTriggerTimesNode();
        Task<Node> GetAvailableNode(string schedName, string schedInstanceId);
        Task IncreaseTriggerTime(string schedName, string schedInstanceId);

        #endregion
    }
}