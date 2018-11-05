using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Swarm.Client.Listener
{
    public class ExitListener
    {
        private readonly IProcessStore _processStore;
        private readonly ILogger _logger;

        public ExitListener(IProcessStore processStore, ILoggerFactory loggerFactory)
        {
            _processStore = processStore;
            _logger = loggerFactory.CreateLogger<KillAllListener>();
        }

        public void Handle()
        {
            // TODO: Wait all process exit, and all ISwarmJob exit.
            foreach (JobProcess jp in _processStore.GetProcesses())
            {
                try
                {
                    Process.GetProcessById(jp.ProcessId).Kill();
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        $"Kill job {jp.JobId}, trace {jp.TraceId}, sharding {jp.Sharding}] process failed: {e.Message}.");
                }
            }
            
            _logger.LogInformation("Exit by server");
            Environment.Exit(0);
        }
    }
}