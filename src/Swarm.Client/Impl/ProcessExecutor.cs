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
        private readonly ILogger _logger;
        private readonly IProcessStore _store;

        public ProcessExecutor(IProcessStore store)
        {
            _store = store;
            if (_logger == null)
            {
                _logger = new ConsoleLogger("ProcessExecutor", (cat, lv) => lv > LogLevel.Debug, true);
            }
        }

        public Task<int> Execute(JobContext context, Action<string, string, string> logger)
        {
            var key = new ProcessKey(context.JobId, context.TraceId, context.CurrentSharding);

            if (_store.Exists(key))
            {
                throw new SwarmClientException(
                    $"[{context.JobId}, {context.TraceId}, {context.CurrentSharding}] is running");
            }

            if (_store.Count(context.JobId) > 1)
            {
                if (context.AllowConcurrent)
                {
                    throw new SwarmClientException("job is running");
                }
            }

            var application = context.Parameters.GetValue(SwarmConts.ApplicationProperty);
            if (string.IsNullOrWhiteSpace(application))
            {
                throw new SwarmClientException("application path is null");
            }

            var arguments = context.Parameters.GetValue(SwarmConts.ArgumentsProperty);
            arguments = ReplaceEnvironments(arguments);
            var logPattern = context.Parameters.GetValue(SwarmConts.LogPatternProperty);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(application, arguments)
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
            process.Start();
            if (!string.IsNullOrWhiteSpace(logPattern))
            {
                process.BeginOutputReadLine();
            }
            _store.Add(new JobProcess
            {
                JobId = context.JobId,
                TraceId = context.TraceId,
                Sharding = context.CurrentSharding,
                Application = application,
                Arguments = arguments,
                StartAt = DateTimeOffset.Now,
                ProcessId = process.Id
            });
            _logger.LogInformation(
                $"Start process [{context.Name}, {context.Group}] PID: {process.Id}.");
            process.WaitForExit();
            _store.Remove(key);
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