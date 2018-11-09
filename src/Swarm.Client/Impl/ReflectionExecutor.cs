using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic;

namespace Swarm.Client.Impl
{
    public class ReflectionExecutor : ExecutorBase
    {
        public ReflectionExecutor(IProcessStore store, ILoggerFactory loggerFactory,
            IOptions<SwarmClientOptions> options) : base(store, loggerFactory, options)
        {
        }

        public override async Task Execute(JobContext context, HubConnection connection)
        {
            var className = context.Parameters[SwarmConsts.ClassProperty];
            var type = Type.GetType(className);
            if (type != null)
            {
                if (Activator.CreateInstance(type) is ISwarmJob instance)
                {
                    try
                    {
                        await OnRunning(context, connection, Process.GetCurrentProcess().Id);
                        await instance.Handle(context);
                        await OnExited(context, connection, Process.GetCurrentProcess().Id, $"Exit: 0");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Execute job [{context.Name}, {context.Group}] failed: {ex}.");
                    }
                }

                await OnExited(context, connection, Process.GetCurrentProcess().Id, $"Exit: -1");

                throw new SwarmClientException($"{className} is not implement ISwarmJob.");
            }

            await OnExited(context, connection, Process.GetCurrentProcess().Id, $"Exit: -1");
            throw new SwarmClientException($"{className} unfounded.");
        }

        public override void Dispose()
        {
        }
    }
}