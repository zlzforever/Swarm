using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Swarm.Basic;
using Swarm.Client.Impl;

namespace Swarm.Client
{
    public class SwarmClient
    {
        private readonly ILogger _logger;
        private int _retryTimes;
        private bool _isRunning;
        private CancellationToken _cancellationToken;

        /// <summary>
        /// 分组
        /// </summary>
        public string Group { get; }

        /// <summary>
        /// Scheduler.NET 服务地址
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Client 名称
        /// </summary>
        public string Name { get; }

        public string AccessToken { get; }

        /// <summary>
        /// 服务连接重试次数
        /// </summary>
        public int RetryTimes { get; set; } = 3600;

        protected SwarmClient()
        {
            Name = string.IsNullOrWhiteSpace(Name) ? Dns.GetHostName() : Name;
            if (_logger == null)
            {
                _logger = new ConsoleLogger("SwarmClient", (cat, lv) => lv > LogLevel.Debug, true);
            }
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="url">服务地址</param>
        /// <param name="name">名称</param>
        /// <param name="group">分组</param>
        public SwarmClient(string url, string accessToken, string name, string group = "DEFAULT") : this()
        {
            Group = group;
            Url = new Uri(url).ToString();
            Name = name;
            AccessToken = accessToken;
        }

        public void Start(CancellationToken cancellationToken = default)
        {
            if (_isRunning)
            {
                throw new SwarmClientException("Client is running.");
            }

            _cancellationToken = cancellationToken;
            _isRunning = true;
            Task.Factory.StartNew(async () =>
            {
                var times = Interlocked.Increment(ref _retryTimes);
                while (times <= RetryTimes)
                {
                    bool exit = false;
                    while (times <= RetryTimes)
                    {
                        var connection = new HubConnectionBuilder()
                            .WithUrl($"{Url}client/?group={Group}&name={Name}&ip=127.0.0.1", config =>
                            {
                                config.Headers = new Dictionary<string, string>
                                {
                                    {SwarmConts.AccessTokenHeader, AccessToken}
                                };
                            })
                            .Build();
                        try
                        {
                            connection.Closed += e =>
                            {
                                exit = true;
                                _logger.LogWarning($"Disconnected from server: {e?.Message}.");
                                return Task.CompletedTask;
                            };
                            OnTrigger(connection);
                            await connection.StartAsync(cancellationToken);
                            break;
                        }
                        catch (Exception e) when (e.InnerException?.InnerException is SocketException)
                        {
                            await connection.StopAsync(cancellationToken);
                            await connection.DisposeAsync();
                            var exception = (SocketException) e.InnerException.InnerException;
                            if (exception.SocketErrorCode == SocketError.TimedOut ||
                                exception.SocketErrorCode == SocketError.ConnectionRefused)
                            {
                                Thread.Sleep(1000);

                                if (times <= RetryTimes)
                                {
                                    _logger.LogInformation("Retry to connect server.");
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }

                    while (!cancellationToken.IsCancellationRequested && !exit)
                    {
                        cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    }
                }
            }, cancellationToken).ContinueWith(t => { _isRunning = false; }, cancellationToken);
        }

        public void Stop()
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        private void OnTrigger(HubConnection connection)
        {
            connection.On<JobContext>("Trigger", async context =>
            {
                if (context == null)
                {
                    _logger.LogError("Receive null context.");
                    return;
                }

                var delay = (context.FireTimeUtc - DateTime.UtcNow).TotalSeconds;
                if (delay > 10)
                {
                    _logger.LogError($"Trigger job [{context.Name}, {context.Group}] timeout: {delay}.");
                    await connection.SendAsync("StateChanged", context.JobId, context.TraceId, State.Exit, "Timeout",
                        _cancellationToken);
                    return;
                }

                try
                {
                    _logger.LogInformation($"Try execute job: [{context.JobId}]");

                    await connection.SendAsync("StateChanged", context.JobId, context.TraceId, State.Running, "",
                        _cancellationToken);

                    var exitCode = await ExecutorFactory.Create(context.Executor).Execute(context,
                        async (jobId, traceId, msg) =>
                        {
                            await connection.SendAsync("OnLog", jobId, traceId, msg,
                                cancellationToken: _cancellationToken);
                        });

                    await connection.SendAsync("StateChanged", context.JobId, context.TraceId, State.Exit,
                        $"Exit: {exitCode}",
                        _cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Execute job [{context.Name}, {context.Group}] failed.");
                    await connection.SendAsync("StateChanged", context.JobId, context.TraceId, State.Exit,
                        $"Failed: {ex.Message}",
                        _cancellationToken);
                }
            });
        }
    }
}