using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Swarm.Client.Listener
{
    public class KillAllListener
    {
        private readonly IProcessStore _processStore;
        private readonly ILogger _logger;

        public KillAllListener(IProcessStore processStore, ILoggerFactory loggerFactory)
        {
            _processStore = processStore;
            _logger = loggerFactory.CreateLogger<KillAllListener>();
        }

        public void Handle(string jobId)
        {
            var pros = _processStore.GetProcesses(jobId);

            foreach (var proc in pros)
            {
                try
                {
                    Process.GetProcessById(proc.ProcessId).Kill();
                    _processStore.Remove(new ProcessKey(proc.JobId, proc.TraceId, proc.Sharding));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Kill PID {proc.ProcessId} Job {jobId} failed: {ex.Message}.");
                }
            }
        }
    }
}