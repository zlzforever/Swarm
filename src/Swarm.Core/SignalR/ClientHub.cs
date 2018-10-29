using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Core.Common;

namespace Swarm.Core.SignalR
{
    public class ClientHub : Hub
    {
        private readonly SwarmOptions _options;
        private readonly ILogger _logger;
        private readonly IStore _store;

        public ClientHub(IOptions<SwarmOptions> options,
            IStore store, ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<ClientHub>();
            _store = store;
        }

        public async Task StateChanged(string jobId, string traceId, int sharding, State state, string msg)
        {
            var ci = Context.GetClient();
            await _store.ChangeJobState(traceId, ci.Name, sharding, state, msg);
            switch (state)
            {
                case State.Exit:
                    if (await _store.CheckJobExited(jobId))
                    {
                        await _store.ChangeJobState(jobId, State.Exit);
                    }

                    break;
                case State.Running:
                    await _store.ChangeJobState(jobId, State.Running);
                    break;
            }
        }

        public async Task OnLog(string id, string traceId, string msg)
        {
            await _store.AddLog(id, traceId, msg);
        }

        public override async Task OnConnectedAsync()
        {
            var ci = Context.GetClient();

            var skip = false;
            if (!Context.GetHttpRequest().IsAccess(_options))
            {
                _logger.LogWarning(
                    $"[{Context.ConnectionId}, {ci.Name}, {ci.Group}, {ci.Ip}] access denied.");
                skip = true;
            }

            await base.OnConnectedAsync();

            if (!skip && (string.IsNullOrWhiteSpace(ci.Name) || string.IsNullOrWhiteSpace(ci.Ip)))
            {
                _logger.LogWarning(
                    $"[{Context.ConnectionId}, {ci.Name}, {ci.Group}, {ci.Ip}] is not valid.");
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
                            $"[{Context.ConnectionId}, {ci.Name}, {ci.Group}, {ci.Ip}] register success.");
                        return;
                    }

                    if (client.IsConnected)
                    {
                        _logger.LogInformation(
                            $"[{Context.ConnectionId}, {ci.Name}, {ci.Group}, {ci.Ip}] is connected.");
                    }
                    else
                    {
                        await _store.ConnectClient(ci.Name, ci.Group, ci.ConnectionId);
                        _logger.LogInformation(
                            $"[{Context.ConnectionId}, {ci.Name}, {ci.Group}, {ci.Ip}] register success.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"[{Context.ConnectionId}, {ci.Name}, {ci.Group}, {ci.Ip}] register failed: {ex.Message}.");
                }
            }

            Context.Abort();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var ci = Context.GetClient();

            try
            {
                await _store.DisconnectClient(ci.ConnectionId);
                await base.OnDisconnectedAsync(exception);
                _logger.LogInformation($"[{Context.ConnectionId}, {ci.Name}, {ci.Group}, {ci.Ip}] disconnected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Disconnect connection failed: {ex.Message}.");
            }
        }
    }
}