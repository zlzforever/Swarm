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

        private static readonly Dictionary<string, Dictionary<string, Process>> _processes =
            new Dictionary<string, Dictionary<string, Process>>();

        public ProcessExecutor()
        {
            if (_logger == null)
            {
                _logger = new ConsoleLogger("ProcessExecutor", (cat, lv) => lv > LogLevel.Debug, true);
            }
        }

        public Task<int> Execute(JobContext context, Action<string, string, string> logger)
        {
            if (!_processes.ContainsKey(context.JobId))
            {
                _processes.Add(context.JobId, new Dictionary<string, Process>());
            }

            if (_processes[context.JobId].Count > 1)
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
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogInformation(e.Data);
                        if (Regex.IsMatch( e.Data,logPattern))
                        {
                            logger?.Invoke(context.JobId, context.TraceId, e.Data);
                        }
                    }
                };
            }

            _processes[context.JobId].Add(context.TraceId, process);
            process.Start();
            if (!string.IsNullOrWhiteSpace(logPattern))
            {
                process.BeginOutputReadLine();
            }

            _logger.LogInformation(
                $"Start process [{context.Name}, {context.Group}] PID: {process.Id}.");
            process.WaitForExit();
            _processes[context.JobId].Remove(context.TraceId);
            _logger.LogInformation(
                $"[{context.Name}, {context.Group}] PID: {process.Id} exited.");
            return Task.FromResult(process.ExitCode);
        }
    }
}