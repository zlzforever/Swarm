using System;
using System.Linq;
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
        private readonly IStore _store;

        public ClientHub(IOptions<SwarmOptions> options,
            IStore store, ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<ClientHub>();
            _store = store;
        }

        public async Task StateChanged(string jobId, string traceId, State state, string msg)
        {
            var ci = Context.GetClient();
            await _store.ChangeJobState(traceId, ci.Name, state, msg);
            switch (state)
            {
                case State.Exit:
                    CheckAndUpdateJobState(jobId, traceId);
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
                    var success = await _store.RegisterClient(ci);
                    if (success)
                    {
                        await base.OnConnectedAsync();
                    }

                    _logger.LogInformation(
                        $"[{Context.ConnectionId}, {ci.Name}, {ci.Group}, {ci.Ip}] register success.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"[{Context.ConnectionId}, {ci.Name}, {ci.Group}, {ci.Ip}] register failed: {ex.Message}.");
                }
            }
            else
            {
                Context.Abort();
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var ci = Context.GetClient();

            try
            {
                await _store.RemoveClient(Context.ConnectionId);
                await base.OnDisconnectedAsync(exception);
                _logger.LogInformation($"[{Context.ConnectionId}, {ci.Name}, {ci.Group}, {ci.Ip}] disconnected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Disconnect connection failed: {ex.Message}.");
            }
        }

        private void CheckAndUpdateJobState(string id, string traceId)
        {
            //TODO: IF ALL TRACE COMPLETED, UPDATE JOB STATE IN JOB TABLE
        }
    }
}