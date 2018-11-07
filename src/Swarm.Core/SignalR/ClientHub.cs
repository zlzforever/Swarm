using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;
using Swarm.Core.Common;

namespace Swarm.Core.SignalR
{
    public class ClientHub : Hub
    {
        private readonly SwarmOptions _options;
        private readonly ILogger _logger;
        private readonly IClientStore _clientStore;
        private readonly ILogStore _logStore;
        private readonly ISwarmStore _swarmStore;

        public ClientHub(IOptions<SwarmOptions> options, ISwarmStore swarmStore,
            IClientStore store,
            ILogStore logStore, ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<ClientHub>();
            _clientStore = store;
            _logStore = logStore;
            _swarmStore = swarmStore;
        }

        public async Task StateChanged(ClientProcess clientProcess)
        {
            //TODO: Validate jobState
            if (clientProcess == null)
            {
                var ci = Context.GetClient(_options);
                _logger.LogError($"{nameof(clientProcess)} is null from {ci}");
                return;
            }

            await _swarmStore.AddOrUpdateClientProcess(clientProcess);
        }

        public async Task Log(Log log)
        {
            // TODO: VALIDATE LOG
            await _logStore.AddLog(log);
        }

        public async Task Heartbeat()
        {
            var ci = Context.GetClient(_options);
            await _clientStore.ClientHeartbeat(ci.Name, ci.Group);
        }

        public override async Task OnConnectedAsync()
        {
            var ci = Context.GetClient(_options);

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
                    var client = await _clientStore.GetClient(ci.Name, ci.Group);
                    if (client == null)
                    {
                        await _clientStore.AddClient(ci);
                        _logger.LogInformation(
                            $"{ci} register success");
                        return;
                    }

                    // SSN 崩溃导致没有设置 IsConnected 为 false, 所以需要检测心跳
                    if (!client.IsConnected ||
                        (DateTime.Now - (client.LastModificationTime ?? client.CreationTime)).Seconds < 7)
                    {
                        await _clientStore.ConnectClient(ci.Name, ci.Group, ci.ConnectionId);
                        _logger.LogInformation(
                            $"{ci} register success");
                        return;
                    }
                    else
                    {
                        _logger.LogInformation(
                            $"[{ci.Name}, {ci.Group}] is connected already");
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
            var ci = Context.GetClient(_options);

            try
            {
                await _clientStore.DisconnectClient(ci.Name, ci.Group);
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