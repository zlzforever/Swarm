using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Basic.Common;
using Swarm.Basic.Entity;

namespace Swarm.Client.Impl
{
    public abstract class ExecutorBase : IExecutor
    {
        protected readonly ILogger Logger;
        protected readonly IProcessStore Store;
        protected readonly SwarmClientOptions Options;

        public abstract Task Execute(JobContext context, HubConnection connection);

        protected ExecutorBase(IProcessStore store, ILoggerFactory loggerFactory, IOptions<SwarmClientOptions> options)
        {
            Store = store;
            Logger = loggerFactory.CreateLogger<ProcessExecutor>();
            Options = options.Value;
        }

        protected async Task OnRunning(JobContext context, HubConnection connection, int processId)
        {
            await connection.SendAsync("StateChanged", new ClientProcess
            {
                Name = Options.Name,
                Group = Options.Group,
                JobId = context.JobId,
                TraceId = context.TraceId,
                Sharding = context.CurrentSharding,
                State = State.Running,
                Msg = $"Process is running",
                App = context.Parameters.GetValue(SwarmConsts.ApplicationProperty),
                AppArguments = context.Parameters.GetValue(SwarmConsts.ArgumentsProperty),
                ProcessId = processId
            });
        }

        protected async Task OnLog(JobContext context, HubConnection connection, string msg)
        {
            await connection.SendAsync("Log",
                new Log
                {
                    ClientName = Options.Name,
                    ClientGroup = Options.Group,
                    JobId = context.JobId,
                    TraceId = context.TraceId,
                    Msg = msg,
                    Sharding = context.CurrentSharding
                });
        }

        public async Task OnExited(JobContext context, HubConnection connection, int processId, string msg)
        {
            await connection.SendAsync("StateChanged", new ClientProcess
            {
                Name = Options.Name,
                Group = Options.Group,
                JobId = context.JobId,
                TraceId = context.TraceId,
                Sharding = context.CurrentSharding,
                State = State.Exit,
                Msg = msg,
                App = context.Parameters.GetValue(SwarmConsts.ApplicationProperty),
                AppArguments = context.Parameters.GetValue(SwarmConsts.ArgumentsProperty),
                ProcessId = processId
            });
        }
    }
}