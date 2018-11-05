using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;

namespace Swarm.Core.Impl
{
    public class SwarmCluster : ISwarmCluster
    {
        private readonly SwarmOptions _options;
        private readonly ISwarmStore _store;

        public SwarmCluster(IOptions<SwarmOptions> options, ISwarmStore store)
        {
            _options = options.Value;
            _store = store;
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            // Clean all old client connect information
            await _store.DisconnectAllClients();

            while (!cancellationToken.IsCancellationRequested)
            {
                var node = new Node
                {
                    ConnectionString = _options.QuartzConnectionString,
                    SchedInstanceId = _options.SchedInstanceId,
                    Provider = _options.Provider,
                    SchedName = _options.SchedName,
                    TriggerTimes = 0
                };
                await _store.RegisterNode(node);
                await Task.Delay(TimeSpan.FromMilliseconds(5000), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}