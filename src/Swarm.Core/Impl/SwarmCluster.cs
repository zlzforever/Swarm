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

        public async Task Start(CancellationToken token = default)
        {
            do
            {
                token.ThrowIfCancellationRequested();

                await _store.RegisterNode(new Node
                {
                    ConnectionString = _options.QuartzConnectionString,
                    NodeId = _options.NodeId,
                    Provider = _options.Provider,
                    SchedName = _options.Name,
                    TriggerTimes = 0
                });
                await Task.Delay(TimeSpan.FromMilliseconds(5000), token).ConfigureAwait(false);
            } while (true);

            // ReSharper disable once FunctionNeverReturns
        }

        public async Task IncreaseTriggerTime()
        {
            await _store.IncreaseNodeTriggerTime(_options.Name, _options.NodeId);
        }
    }
}