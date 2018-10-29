using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Client.Impl;

namespace Swarm.Client
{
    public class SwarmClient : ISwarmClient
    {
        private readonly ILogger _logger;
        private int _retryTimes;
        private bool _isRunning;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisconncted = true;

        /// <summary>
        /// 分组
        /// </summary>
        public string Group { get; }

        /// <summary>
        /// Scheduler.NET 服务地址
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Client 名称
        /// </summary>
        public string Name { get; }

        public string AccessToken { get; }

        /// <summary>
        /// 服务连接重试次数
        /// </summary>
        public int RetryTimes { get; set; } = 3600;

        public int DetectInterval { get; set; } = 1500;

        private SwarmClient()
        {
            //TODO: Validate data
            Name = string.IsNullOrWhiteSpace(Name) ? Dns.GetHostName() : Name;
            if (_logger == null)
            {
                _logger = new ConsoleLogger("SwarmClient", (cat, lv) => lv > LogLevel.Debug, true);
            }
        }

        public SwarmClient(IOptions<SwarmClientOptions> options, ILoggerFactory loggerFactory) : this()
        {
            var ops = options.Value;
            Name = ops.Name;
            Host = new Uri(ops.Host).ToString();
            Group = ops.Group;
            AccessToken = ops.AccessToken;
            RetryTimes = ops.RetryTimes;
            _logger = loggerFactory.CreateLogger<SwarmClient>();
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="url">服务地址</param>
        /// <param name="accessToken"></param>
        /// <param name="name">名称</param>
        /// <param name="group">分组</param>
        public SwarmClient(string url, string accessToken, string name, string group = "DEFAULT") : this()
        {
            Group = group;
            Host = new Uri(url).ToString();
            Name = name;
            AccessToken = accessToken;
        }

        public Task Start(CancellationToken cancellationToken = default)
        {
            if (_isRunning)
            {
                throw new SwarmClientException("Client is running.");
            }

            CancellationToken token;
            if (cancellationToken == default)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                token = _cancellationTokenSource.Token;
            }
            else
            {
                token = cancellationToken;
            }

            _isRunning = true;

            return Task.Factory.StartNew(async () =>
            {
                while (_retryTimes < RetryTimes && !token.IsCancellationRequested)
                {
                    if (_isDisconncted)
                    {
                        await CreateConnection(token);
                    }
                    else
                    {
                        Interlocked.Exchange(ref _retryTimes, 0);
                    }

                    token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(1500));
                }

                _isRunning = false;
            }, token);
        }


        private async Task<HubConnection> CreateConnection(CancellationToken token)
        {
            HubConnection connection = null;
            while (_retryTimes < RetryTimes && !token.IsCancellationRequested)
            {
                Interlocked.Increment(ref _retryTimes);

                connection = new HubConnectionBuilder()
                    .WithUrl($"{Host}clienthub/?group={Group}&name={Name}&ip=127.0.0.1", config =>
                    {
                        config.Headers = new Dictionary<string, string>
                        {
                            {SwarmConts.AccessTokenHeader, AccessToken}
                        };
                    }).Build();
                connection.Closed += e =>
                {
                    _isDisconncted = true;
                    _logger.LogWarning($"Disconnected from server: {e?.Message}.");
                    return Task.CompletedTask;
                };

                AddListener(connection, token);
                var kv = await connection.StartAsync(token)
                    .ContinueWith(t => new Tuple<bool, string>(t.IsFaulted, t.Exception?.Message), token);
                _isDisconncted = kv.Item1;
                if (_isDisconncted)
                {
                    _logger.LogError($"Connect server failed: {kv.Item2}.");
                    token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(1500));
                }
                else
                {
                    _logger.LogError($"Connect server success.");
                    break;
                }
            }

            return connection;
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();

            // TODO: Wait all process exit, and all ISwarmJob exit.
            foreach (var job in ProcessExecutor.Processes)
            {
                foreach (var proc in job.Value)
                {
                    try
                    {
                        proc.Value.Kill();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Kill job {job.Key}, trace {proc.Key}] process failed: {e.Message}.");
                    }
                }
            }
        }

        private void AddListener(HubConnection connection, CancellationToken token)
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
                        token);
                    return;
                }

                try
                {
                    _logger.LogInformation($"Try execute job: [{context.JobId}]");

                    await connection.SendAsync("StateChanged", context.JobId, context.TraceId, State.Running, "",
                        token);

                    var exitCode = await ExecutorFactory.Create(context.Executor).Execute(context,
                        async (jobId, traceId, msg) =>
                        {
                            await connection.SendAsync("OnLog", jobId, traceId, msg,
                                token);
                        });

                    await connection.SendAsync("StateChanged", context.JobId, context.TraceId, State.Exit,
                        $"Exit: {exitCode}",
                        token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Execute job [{context.Name}, {context.Group}] failed.");
                    await connection.SendAsync("StateChanged", context.JobId, context.TraceId, State.Exit,
                        $"Failed: {ex.Message}",
                        token);
                }
            });

            connection.On("Exit", () =>
            {
                Stop();
                _logger.LogInformation("Exit by server.");
                Environment.Exit(0);
            });
        }
    }
}