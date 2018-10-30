using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Swarm.Basic;
using Swarm.Basic.Common;

namespace Swarm.Client.Impl
{
    public class ProcessExecutor : IExecutor
    {
        internal class ProcessKey
        {
            public string JobId { get; }
            public string TraceId { get; }
            public int Sharding { get; }

            public ProcessKey(string jobId, string traceId, int sharding)
            {
                JobId = jobId;
                TraceId = traceId;
                Sharding = sharding;
            }

            public override int GetHashCode()
            {
                return $"{JobId}_{TraceId}_{Sharding}".GetHashCode();
            }
        }

        private readonly ILogger _logger;

        internal static readonly ConcurrentDictionary<ProcessKey, Process> Processes =
            new ConcurrentDictionary<ProcessKey, Process>();

        public ProcessExecutor()
        {
            if (_logger == null)
            {
                _logger = new ConsoleLogger("ProcessExecutor", (cat, lv) => lv > LogLevel.Debug, true);
            }
        }

        public Task<int> Execute(JobContext context, Action<string, string, string> logger)
        {
            var key = new ProcessKey(context.JobId, context.TraceId, context.CurrentSharding);
            if (Processes.ContainsKey(key))
            {
                throw new SwarmClientException(
                    $"[{context.JobId}, {context.TraceId}, {context.CurrentSharding}] is running");
            }

            if (Processes.Keys.Count(k => k.JobId == context.JobId) > 1)
            {
                if (context.ConcurrentExecutionDisallowed)
                {
                    throw new SwarmClientException("job is running");
                }
            }

            var app = context.Parameters.GetValue(SwarmConts.ApplicationProperty);
            if (string.IsNullOrWhiteSpace(app))
            {
                throw new SwarmClientException("application path is null");
            }

            var arguments = context.Parameters.GetValue(SwarmConts.ArgumentsProperty);
            arguments = ReplaceEnvironments(arguments);
            var logPattern = context.Parameters.GetValue(SwarmConts.LogPatternProperty);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(app, arguments)
                {
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = !string.IsNullOrWhiteSpace(logPattern)
                }
            };
            if (!string.IsNullOrWhiteSpace(logPattern))
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    _logger.LogInformation(e.Data);
                    if (Regex.IsMatch(e.Data, logPattern))
                    {
                        logger?.Invoke(context.JobId, context.TraceId, e.Data);
                    }
                };
            }

            Processes.TryAdd(key, process);
            process.Start();
            if (!string.IsNullOrWhiteSpace(logPattern))
            {
                process.BeginOutputReadLine();
            }

            _logger.LogInformation(
                $"Start process [{context.Name}, {context.Group}] PID: {process.Id}.");
            process.WaitForExit();
            Processes.TryRemove(key, out _);
            _logger.LogInformation(
                $"[{context.Name}, {context.Group}] PID: {process.Id} exited.");
            return Task.FromResult(process.ExitCode);
        }

        private string ReplaceEnvironments(string arguments)
        {
            return arguments.Replace("%root%", AppContext.BaseDirectory);
        }
    }
}