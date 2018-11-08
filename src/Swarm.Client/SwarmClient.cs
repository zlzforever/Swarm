using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Client.Listener;

namespace Swarm.Client
{
    public class SwarmClient : ISwarmClient
    {
        private readonly ILogger _logger;
        private int _retryTimes;
        private bool _isRunning;
        private bool _isDisconnected = true;
        private readonly KillAllListener _killAllListener;
        private readonly KillListener _killListener;
        private readonly ExitListener _exitListener;
        private readonly TriggerListener _triggerListener;

        #region Properties

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

        /// <summary>
        /// 鉴权 Token
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// 本机 IP 地址
        /// </summary>
        public string Ip { get; }

        /// <summary>
        /// 服务连接重试次数
        /// </summary>
        public int RetryTimes { get; }

        /// <summary>
        /// 心跳间隔
        /// </summary>
        public int HeartbeatInterval { get; } = 5000;

        #endregion

        /// <summary>
        /// DI 所使用的构造方法
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="killAllListener"></param>
        /// <param name="killListener"></param>
        /// <param name="exitListener"></param>
        /// <param name="triggerListener"></param>
        public SwarmClient(IOptions<SwarmClientOptions> options, ILoggerFactory loggerFactory,
            KillAllListener killAllListener,
            KillListener killListener,
            ExitListener exitListener,
            TriggerListener triggerListener)
        {
            _logger = loggerFactory.CreateLogger<SwarmClient>();

            var ops = options.Value;
            Name = ops.Name;
            Host = new Uri(ops.Host).ToString();
            Group = ops.Group;
            AccessToken = ops.AccessToken;
            RetryTimes = ops.RetryTimes;
            Ip = ops.Ip;
            HeartbeatInterval = ops.HeartbeatInterval;

            _killAllListener = killAllListener;
            _killListener = killListener;
            _exitListener = exitListener;
            _triggerListener = triggerListener;

            //TODO: Validate data
            Name = string.IsNullOrWhiteSpace(Name) ? Dns.GetHostName() : Name;

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

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _exitListener.Handle();
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            if (_isRunning)
            {
                throw new SwarmClientException("Client is running.");
            }

            _isRunning = true;

            HubConnection conn = null;
            while (_retryTimes < RetryTimes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (conn == null || _isDisconnected)
                {
                    conn = await CreateConnection(cancellationToken);
                }

                await conn.SendAsync("Heartbeat", cancellationToken);
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(HeartbeatInterval));
                _logger.LogInformation("Client heartbeat");
            }

            _isRunning = false;
        }

        private async Task<HubConnection> CreateConnection(CancellationToken token)
        {
            HubConnection connection = null;
            while (_retryTimes < RetryTimes)
            {
                token.ThrowIfCancellationRequested();

                Interlocked.Increment(ref _retryTimes);

                connection = new HubConnectionBuilder()
                    .WithUrl(
                        $"{Host}clientHub/?group={Group}&name={Name}&ip={Ip}&os={Environment.OSVersion}&coreCount={Environment.ProcessorCount}&memory=2048&userId=0",
                        config =>
                        {
                            config.Headers = new Dictionary<string, string>
                            {
                                {SwarmConsts.AccessTokenHeader, AccessToken}
                            };
                        }).AddMessagePackProtocol().Build();
                connection.Closed += e =>
                {
                    _isDisconnected = true;
                    _logger.LogWarning($"Disconnected from server.");
                    return Task.CompletedTask;
                };

                AddListener(connection, token);

                var tuple = await connection.StartAsync(token)
                    .ContinueWith(t => new Tuple<bool, string>(t.IsFaulted, t.Exception?.Message), token);
                _isDisconnected = tuple.Item1;
                if (_isDisconnected)
                {
                    _logger.LogError($"Connect server failed: {tuple.Item2}.");
                    token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(HeartbeatInterval));
                }
                else
                {
                    Interlocked.Exchange(ref _retryTimes, 0);
                    _logger.LogInformation("Connect server success.");
                    break;
                }
            }

            return connection;
        }

        private void AddListener(HubConnection connection, CancellationToken token)
        {
            connection.On<JobContext>("Trigger",
                async context => { await _triggerListener.Handle(connection, context, token); });

            connection.On("Exit", _exitListener.Handle);
            connection.On<string>("Kill", _killAllListener.Handle);
            connection.On<string, string, int>("Kill", _killListener.Handle);
        }
    }
}