using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly ILogger _logger;
        private static readonly Dictionary<string, Process> _processes = new Dictionary<string, Process>();

        public ProcessExecutor()
        {
            if (_logger == null)
            {
                _logger = new ConsoleLogger("ProcessExecutor", (cat, lv) => lv > LogLevel.Debug, true);
            }
        }

        public Task<int> Execute(JobContext context, Action<string, string, string> logger)
        {
            if (_processes.ContainsKey(context.JobId))
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
            var logPattern = context.Parameters.GetValue(SwarmConts.LogPatternProperty);
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(app, arguments)
            {
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = !string.IsNullOrWhiteSpace(logPattern)
            };
            if (!string.IsNullOrWhiteSpace(logPattern))
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data) && Regex.IsMatch(logPattern, e.Data))
                    {
                        logger?.Invoke(context.JobId, context.TraceId, e.Data);
                    }
                };
            }

            _processes.Add(context.JobId, process);
            process.Start();
            if (!string.IsNullOrWhiteSpace(logPattern))
            {
                process.BeginOutputReadLine();
            }

            _logger.LogInformation(
                $"Start process [{context.Name}, {context.Group}] PID: {process.Id}.");
            process.WaitForExit();
            _processes.Remove(context.JobId);
            _logger.LogInformation(
                $"[{context.Name}, {context.Group}] PID: {process.Id} exited.");
            return Task.FromResult(process.ExitCode);
        }
    }
}