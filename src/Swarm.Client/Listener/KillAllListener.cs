using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Swarm.Client.Listener
{
    public class KillAllListener : IDisposable
    {
        private readonly IProcessStore _processStore;
        private readonly ILogger _logger;

        public KillAllListener(IProcessStore processStore, ILogger<KillAllListener> logger)
        {
            _processStore = processStore;
            _logger = logger;
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

        public void Dispose()
        {
        }
    }
}