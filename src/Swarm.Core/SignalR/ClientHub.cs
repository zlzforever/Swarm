using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Basic.Entity;
using Swarm.Core.Common;

namespace Swarm.Core.SignalR
{
    public class ClientHub : Hub
    {
        private readonly SwarmOptions _options;
        private readonly ILogger _logger;
        private readonly ISwarmStore _store;
        private readonly ILogStore _logStore;
        
        public ClientHub(IOptions<SwarmOptions> options,
            ISwarmStore store,ILogStore logStore, ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<ClientHub>();
            _store = store;
            _logStore = logStore;
        }

        public async Task StateChanged(JobState jobState)
        {
            //TODO: Validate jobState
            if (jobState == null)
            {
                var ci= Context.GetClient();
                _logger.LogError($"{nameof(jobState)} is null from {ci}");
                return;
            }
            var oldJobState = await _store.GetJobState(jobState.TraceId,jobState.Client, jobState.Sharding);
            if (oldJobState == null)
            {
                var ci= Context.GetClient();
                _logger.LogError($"{ci} {jobState.TraceId}, {jobState.Client}, {jobState.Sharding} is not exists");
                return;
            }
            else
            {
                jobState.Id = oldJobState.Id;
                await _store.UpdateJobState(jobState);
            }

            switch (jobState.State)
            {
                case State.Exit:
                    if (await _store.IsJobExited(jobState.JobId))
                    {
                        await _store.ChangeJobState(jobState.JobId, State.Exit);
                    }

                    break;
                case State.Running:
                    await _store.ChangeJobState(jobState.JobId, State.Running);
                    break;
            }
        }

        public async Task OnLog(Log log)
        {
            // TODO: VALIDATE LOG
            await _logStore.AddLog(log);
        }

        public override async Task OnConnectedAsync()
        {
            var ci = Context.GetClient();

            var skip = false;
            if (!Context.GetHttpRequest().IsAccess(_options))
            {
                _logger.LogWarning(
                    $"{ci} access denied");
                skip = true;
            }

            await base.OnConnectedAsync();

            if (!skip && (string.IsNullOrWhiteSpace(ci.Name) || string.IsNullOrWhiteSpace(ci.Ip)))
            {
                _logger.LogWarning(
                    $"{ci} is not valid");
                skip = true;
            }

            if (!skip)
            {
                try
                {
                    var client = await _store.GetClient(ci.Name, ci.Group);
                    if (client == null)
                    {
                        ci.IsConnected = true;
                        await _store.AddClient(ci);
                        _logger.LogInformation(
                            $"{ci} register success");
                        return;
                    }

                    if (client.IsConnected)
                    {
                        _logger.LogInformation(
                            $"{ci} is connected");
                    }
                    else
                    {
                        await _store.ConnectClient(ci.Name, ci.Group, ci.ConnectionId);
                        _logger.LogInformation(
                            $"{ci} register success");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"{ci} register failed: {ex.Message}");
                }
            }

            Context.Abort();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var ci = Context.GetClient();

            try
            {
                await _store.DisconnectClient(ci.Name, ci.Group);
                await base.OnDisconnectedAsync(exception);
                _logger.LogInformation($"{ci} disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Disconnect connection failed: {ex.Message}");
            }
        }
    }
}