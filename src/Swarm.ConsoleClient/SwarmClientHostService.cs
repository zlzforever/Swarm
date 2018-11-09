using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swarm.Client;

namespace Swarm.ConsoleClient
{
    public class SwarmClientHostService : IHostedService
    {
        private readonly ISwarmClient _client;
        private Stream _processStream;
        private readonly ILogger _logger;
        private CancellationTokenSource _cancellationTokenSource;

        public SwarmClientHostService(ISwarmClient client, ILogger<SwarmClientHostService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _processStream = File.Open(Path.Combine(AppContext.BaseDirectory, "processId"), FileMode.OpenOrCreate,
                    FileAccess.ReadWrite);
            }
            catch
            {
                _logger.LogError("SwarmClient is running");
                return Task.CompletedTask;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _client.Run(_cancellationTokenSource.Token);
            _logger.LogInformation("ConsoleClient start");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            while (_client.IsRunning)
            {
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(100));
            }

            _processStream.Dispose();
            _logger.LogInformation("ConsoleClient stopped");
            return Task.CompletedTask;
        }
    }
}