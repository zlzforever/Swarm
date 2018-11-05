using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Basic.Common;
using Swarm.Basic.Entity;

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

            var delay = (context.FireTimeUtc - DateTime.UtcNow).TotalSeconds;
            if (delay > 10)
            {
                _logger.LogError(
                    $"Trigger job [{context.Name}, {context.Group}, {context.TraceId}, {context.CurrentSharding}] timeout: {delay}.");
                await connection.SendAsync("StateChanged", new JobState
                    {
                        JobId = context.JobId,
                        TraceId = context.TraceId,
                        Sharding = context.CurrentSharding,
                        State = State.Exit,
                        Client = _options.Name,
                        Msg = "Timeout"
                    }
                    , token);
                return;
            }

            try
            {
                _logger.LogInformation(
                    $"Try execute job [{context.Name}, {context.Group}, {context.TraceId}, {context.CurrentSharding}]");

                await connection.SendAsync("StateChanged", new JobState
                {
                    JobId = context.JobId,
                    TraceId = context.TraceId,
                    Sharding = context.CurrentSharding,
                    Client = _options.Name,
                    State = State.Running
                }, token);

                Enum.TryParse(context.Parameters.GetValue(SwarmConts.ExecutorProperty), out Executor executor);
                var exitCode = await _executorFactory.Create(executor).Execute(context,
                    async (jobId, traceId, msg) =>
                    {
                        await connection.SendAsync("OnLog", new Log {JobId = jobId, TraceId = traceId, Msg = msg},
                            token);
                    });

                await connection.SendAsync("StateChanged", new JobState
                {
                    JobId = context.JobId,
                    TraceId = context.TraceId,
                    Sharding = context.CurrentSharding,
                    Client = _options.Name,
                    State = State.Exit,
                    Msg = $"Exit: {exitCode}"
                }, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Execute job [{context.Name}, {context.Group}] failed.");
                await connection.SendAsync("StateChanged", new JobState
                {
                    JobId = context.JobId,
                    TraceId = context.TraceId,
                    Sharding = context.CurrentSharding,
                    Client = _options.Name,
                    State = State.Exit,
                    Msg = $"Failed: {ex.Message}"
                }, token);
            }
        }
    }
}