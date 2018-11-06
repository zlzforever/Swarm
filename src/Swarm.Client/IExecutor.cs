using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Swarm.Basic;

namespace Swarm.Client
{
    public interface IExecutor
    {
        Task Execute(JobContext context, HubConnection connection);

        Task OnExited(JobContext context, HubConnection connection, int processId, string msg);
    }
}