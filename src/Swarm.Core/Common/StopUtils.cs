using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Swarm.Basic;
using Swarm.Core.Common.Internal;
using Swarm.Core.SignalR;

namespace Swarm.Core.Common
{
    public class StopUtils
    {
        public static async Task Stop(string id)
        {
            var store = Ioc.GetRequiredService<IStore>();
            var job = await store.GetJob(id);
            if (job != null)
            {
                var hubContext = Ioc.GetRequiredService<IHubContext<ClientHub>>();
                var states = await store.GetCurrentJobStates(id);
                var clients = (await store.GetClients(job.Group)).ToList();
                foreach (var jobState in states)
                {
                    if (jobState.State != State.Exit)
                    {
                        var client = clients.FirstOrDefault(c => c.Name == jobState.Client);
                        if (client != null)
                        {
                            await hubContext.Clients.Client(client.ConnectionId).SendAsync("ExitJob", job.Id);
                        }
                    }
                }
            }
        }
    }
}