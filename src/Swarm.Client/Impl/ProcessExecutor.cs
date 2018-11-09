using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Basic.Common;

namespace Swarm.Client.Impl
{
    public class ProcessExecutor : ExecutorBase
    {
        public ProcessExecutor(IProcessStore store, ILoggerFactory loggerFactory, IOptions<SwarmClientOptions> options)
            : base(store, loggerFactory, options)
        {
        }

        public override async Task Execute(JobContext context, HubConnection connection)
        {
            var key = new ProcessKey(context.JobId, context.TraceId, context.CurrentSharding);

            if (Store.Exists(key))
            {
                throw new SwarmClientException(
                    $"[{context.JobId}, {context.TraceId}, {context.CurrentSharding}] is running");
            }

            if (Store.Count(context.JobId) > 1)
            {
                if (context.AllowConcurrent)
                {
                    throw new SwarmClientException("job is running");
                }
            }

            var application = context.Parameters.GetValue(SwarmConsts.ApplicationProperty);
            if (string.IsNullOrWhiteSpace(application))
            {
                throw new SwarmClientException("application path is null");
            }

            var arguments = context.Parameters.GetValue(SwarmConsts.ArgumentsProperty);
            arguments = ReplaceEnvironments(arguments);
            var logPattern = context.Parameters.GetValue(SwarmConsts.LogPatternProperty);
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
                process.OutputDataReceived += async (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    if (Regex.IsMatch(e.Data, logPattern))
                    {
                        await OnLog(context, connection, e.Data);
                    }
                };
            }

            process.Start();

            await OnRunning(context, connection, process.Id);

            if (!string.IsNullOrWhiteSpace(logPattern))
            {
                process.BeginOutputReadLine();
            }

            Store.Add(new JobProcess
            {
                JobId = context.JobId,
                TraceId = context.TraceId,
                Sharding = context.CurrentSharding,
                Application = application,
                Arguments = arguments,
                StartAt = DateTimeOffset.Now,
                ProcessId = process.Id
            });

            Logger.LogInformation(
                $"Start process [{context.Name}, {context.Group}] PID: {process.Id}.");

            process.WaitForExit();

            Store.Remove(key);

            Logger.LogInformation(
                $"[{context.Name}, {context.Group}] PID: {process.Id} exited.");

            await OnExited(context, connection, process.Id, $"Exit: {process.ExitCode}");
        }

        public override void Dispose()
        {
            // TODO: Wait all process exit, and all ISwarmJob exit.
            foreach (JobProcess jp in Store.GetProcesses())
            {
                try
                {
                    Process.GetProcessById(jp.ProcessId).Kill();
                }
                catch (Exception e)
                {
                    Logger.LogError(e,
                        $"Kill job {jp.JobId}, trace {jp.TraceId}, sharding {jp.Sharding}] process failed: {e.Message}.");
                }
            }
        }

        private string ReplaceEnvironments(string arguments)
        {
            return arguments.Replace("%root%", AppContext.BaseDirectory);
        }
    }
}