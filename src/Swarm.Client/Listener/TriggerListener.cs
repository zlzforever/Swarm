using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Basic.Common;

namespace Swarm.Client.Listener
{
    public class TriggerListener
    {
        private readonly ILogger _logger;
        private readonly SwarmClientOptions _options;
        private readonly IExecutorFactory _executorFactory;

        public TriggerListener(ILoggerFactory loggerFactory,
            IOptions<SwarmClientOptions> options, IExecutorFactory executorFactory)
        {
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<KillAllListener>();
            _executorFactory = executorFactory;
        }

        public async Task Handle(HubConnection connection, JobContext context, CancellationToken token = default)
        {
            if (context == null)
            {
                _logger.LogError("Receive null context.");
                return;
            }

            Enum.TryParse(context.Parameters.GetValue(SwarmConsts.ExecutorProperty), out Executor executor);
            var executorInstance = _executorFactory.Create(executor);
            if (executorInstance == null)
            {
                _logger.LogError($"Executor {executor} is not support yet");
                return;
            }

            var delay = (context.FireTimeUtc - DateTime.UtcNow).TotalSeconds;
            if (delay > 10)
            {
                _logger.LogError(
                    $"Trigger job [{context.Name}, {context.Group}, {context.TraceId}, {context.CurrentSharding}] timeout: {delay}.");

                await executorInstance.OnExited(context, connection, 0, "Timeout from server");
                return;
            }

            try
            {
                _logger.LogInformation(
                    $"Try execute job [{context.Name}, {context.Group}, {context.TraceId}, {context.CurrentSharding}]");
                await executorInstance.Execute(context, connection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Execute job [{context.Name}, {context.Group}] failed.");

                await executorInstance.OnExited(context, connection, 0, $"Failed: {ex.Message}");
            }
        }
    }
}