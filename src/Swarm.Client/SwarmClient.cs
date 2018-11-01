using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Basic.Entity;
using Swarm.Client.Impl;

namespace Swarm.Client
{
    public class SwarmClient : ISwarmClient
    {
        private ILogger _logger;
        private int _retryTimes;
        private bool _isRunning;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisconnected = true;
        private   IProcessStore _processStore;
        private readonly IExecutorFactory _executorFactory;
        
        /// <summary>
        /// 分组
        /// </summary>
        public string Group { get; private set; }

        /// <summary>
        /// Scheduler.NET 服务地址
        /// </summary>
        public string Host { get; private set; }

        /// <summary>
        /// Client 名称
        /// </summary>
        public string Name { get; private set; }

        public string AccessToken { get; private set; }

        public string Ip { get; private set; }

        /// <summary>
        /// 服务连接重试次数
        /// </summary>
        public int RetryTimes { get; set; } = 3600;

        public int DetectInterval { get; set; } = 1500;

        /// <summary>
        /// DI 所使用的构造方法
        /// </summary>
        /// <param name="options"></param>
        /// <param name="processStore"></param>
        /// <param name="loggerFactory"></param>
        public SwarmClient(IOptions<SwarmClientOptions> options, IProcessStore processStore, IExecutorFactory executorFactory, ILoggerFactory loggerFactory)
        {
            var ops = options.Value;
            Name = ops.Name;
            Host = new Uri(ops.Host).ToString();
            Group = ops.Group;
            AccessToken = ops.AccessToken;
            RetryTimes = ops.RetryTimes;
            Ip = ops.Ip;
            _logger = loggerFactory.CreateLogger<SwarmClient>();
            _processStore = processStore;
            _executorFactory = executorFactory;
            
            //TODO: Validate data
            Name = string.IsNullOrWhiteSpace(Name) ? Dns.GetHostName() : Name;

            if (_logger == null)
            {
                _logger = new ConsoleLogger("SwarmClient", (cat, lv) => lv > LogLevel.Debug, true);
            }

            if (string.IsNullOrWhiteSpace(Ip))
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i =>
                    (i.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) &&
                    i.OperationalStatus == OperationalStatus.Up);
                if (interfaces != null)
                {
                    Ip = interfaces.GetIPProperties().UnicastAddresses.FirstOrDefault(a =>
                            a.IPv4Mask?.ToString() != "255.255.255.255" &&
                            a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address
                        .ToString();
                }
            }

            if (string.IsNullOrWhiteSpace(Ip))
            {
                Ip = "127.0.0.1";
            }
        }

//        /// <summary>
//        /// 不依赖 DI 的构造方法
//        /// </summary>
//        /// <param name="url">服务地址</param>
//        /// <param name="accessToken"></param>
//        /// <param name="name">名称</param>
//        /// <param name="ip"></param>
//        /// <param name="group">分组</param>
//        public SwarmClient(string url, string accessToken, string name, string ip, string group = "DEFAULT")
//        {
//            Group = group;
//            Host = new Uri(url).ToString();
//            Name = name;
//            AccessToken = accessToken;
//            Ip = ip;
//
//            Init();
//        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _cancellationTokenSource.Cancel();

            // TODO: Wait all process exit, and all ISwarmJob exit.
            foreach (JobProcess jp in _processStore.GetProcesses())
            {
                try
                {
                    Process.GetProcessById(jp.ProcessId).Kill();
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        $"Kill job {jp.JobId}, trace {jp.TraceId}, sharding {jp.Sharding}] process failed: {e.Message}.");
                }
            }

            while (_isRunning)
            {
                _cancellationTokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(DetectInterval));
            }
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
                    if (_isDisconnected)
                    {
                        await CreateConnection(token);
                    }
                    else
                    {
                        Interlocked.Exchange(ref _retryTimes, 0);
                    }

                    token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(DetectInterval));
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
                    .WithUrl($"{Host}clientHub/?group={Group}&name={Name}&ip={Ip}", config =>
                    {
                        config.Headers = new Dictionary<string, string>
                        {
                            {SwarmConts.AccessTokenHeader, AccessToken}
                        };
                    }).Build();
                connection.Closed += e =>
                {
                    _isDisconnected = true;
                    _logger.LogWarning($"Disconnected from server: {e?.Message}.");
                    return Task.CompletedTask;
                };

                AddListener(connection, token);
                var kv = await connection.StartAsync(token)
                    .ContinueWith(t => new Tuple<bool, string>(t.IsFaulted, t.Exception?.Message), token);
                _isDisconnected = kv.Item1;
                if (_isDisconnected)
                {
                    _logger.LogError($"Connect server failed: {kv.Item2}.");
                    token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(DetectInterval));
                }
                else
                {
                    _logger.LogInformation("Connect server success.");
                    break;
                }
            }

            return connection;
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
                    _logger.LogError($"Trigger job [{context.Name}, {context.Group}, {context.TraceId}, {context.CurrentSharding}] timeout: {delay}.");
                    await connection.SendAsync("StateChanged", new JobState
                        {
                            JobId = context.JobId,
                            TraceId = context.TraceId,
                            Sharding = context.CurrentSharding,
                            State = State.Exit,
                            Client = Name,
                            Msg = "Timeout"
                        }
                        , token);
                    return;
                }

                try
                {
                    _logger.LogInformation($"Try execute job [{context.Name}, {context.Group}, {context.TraceId}, {context.CurrentSharding}]");

                    await connection.SendAsync("StateChanged", new JobState
                        {
                            JobId = context.JobId,
                            TraceId = context.TraceId,
                            Sharding = context.CurrentSharding,
                            Client = Name,
                            State = State.Running
                        }, token);

                    var exitCode = _executorFactory .Create(context.Executor).Execute(context,
                        async (jobId, traceId, msg) =>
                        {
                            await connection.SendAsync("OnLog", new Log {JobId = jobId, TraceId = traceId, Msg = msg},
                                token);
                        });

                    await connection.SendAsync("StateChanged", new JobState
                    {
                        JobId = context.JobId,
                        TraceId = context.TraceId,
                        Sharding = context.CurrentSharding,
                        Client = Name,
                        State = State.Exit,
                        Msg = $"Exit: {exitCode}"
                    }, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Execute job [{context.Name}, {context.Group}] failed.");
                    await connection.SendAsync("StateChanged", new JobState
                    {
                        JobId = context.JobId,
                        TraceId = context.TraceId,
                        Sharding = context.CurrentSharding,
                        Client = Name,
                        State = State.Exit,
                        Msg = $"Failed: {ex.Message}"
                    }, token);
                }
            });

            connection.On("Exit", () =>
            {
                Stop();
                _logger.LogInformation("Exit by server.");
                Environment.Exit(0);
            });

            connection.On<string>("Kill", jobId =>
            {
                var pros = _processStore.GetProcesses(jobId);

                foreach (var proc in pros)
                {
                    try
                    {
                        Process.GetProcessById(proc.ProcessId).Kill();
                        _processStore.Remove(new ProcessKey(proc.JobId,proc.TraceId,proc.Sharding));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Kill PID {proc.ProcessId} Job {jobId} failed: {ex.Message}.");
                    }
                }
            });

            connection.On<string, string, int>("Kill", (jobId, traceId, sharding) =>
            {
                var key = new ProcessKey(jobId, traceId, sharding);
                var proc = _processStore.GetProcess(key);
                if (proc==null) return;
  
                try
                {
                    Process.GetProcessById(proc.ProcessId).Kill();
                    _processStore.Remove(key);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Kill PID {proc.ProcessId} Job {jobId} failed: {ex.Message}.");
                }
            });
        }
    }
}