using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Swarm.Client.Listener
{
    public class KillListener : IDisposable
    {
        private readonly IProcessStore _processStore;
        private readonly ILogger _logger;

        public KillListener(IProcessStore processStore, ILoggerFactory loggerFactory)
        {
            _processStore = processStore;
            _logger = loggerFactory.CreateLogger<KillAllListener>();
        }

        public void Handle(string jobId, string traceId, int sharding)
        {
            var key = new ProcessKey(jobId, traceId, sharding);
            var proc = _processStore.GetProcess(key);
            if (proc == null) return;

            try
            {
                Process.GetProcessById(proc.ProcessId).Kill();
                _processStore.Remove(key);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kill PID {proc.ProcessId} Job {jobId} failed: {ex.Message}.");
            }
        }

        public void Dispose()
        {
        }
    }
}