using System.Collections.Generic;
using System.Threading.Tasks;
using Swarm.Basic.Entity;

namespace Swarm.Core
{
    public interface IClientStore
    {
        Task<bool> AddClient(Client client);
        Task RemoveClient(int clientId);
        Task ConnectClient(string name, string group, string connectionId);
        Task<Client> GetClient(string name, string group);
        Task DisconnectClient(string name, string group);
        Task<IEnumerable<Client>> GetAvailableClients(string group);
        Task DisconnectAllClients();
        Task ClientHeartbeat(string name, string group);
        Task<NodeStatistics> GetNodeStatistics();
        Task<int> GetClientCount();
    }
}