using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Basic.Entity;

namespace Swarm.Core.Impl
{
    public class SqlServerSwarmStore : ISwarmStore, ILogStore
    {
        private readonly SwarmOptions _options;

        public SqlServerSwarmStore(IOptions<SwarmOptions> options)
        {
            _options = options.Value;
        }

        #region Client

        public async Task<bool> AddClient(Client client)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.ExecuteAsync(
                           "INSERT INTO [Client] ([SchedName], [SchedInstanceId], [Name], [Group], [ConnectionId], [Ip], [Os], [CoreCount], [Memory], [IsConnected], [CreationTime], [LastModificationTime]) VALUES (@SchedName, @SchedInstanceId, @Name, @Group, @ConnectionId, @Ip, @Os, @CoreCount, @Memory, @IsConnected, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)",
                           client) > 0;
            }
        }

        public async Task RemoveClient(int clientId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "DELETE FROM [Client] WHERE [Id] = @Id",
                    new {Id = clientId});
            }
        }

        public async Task<Client> GetClient(string name, string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                var client = await conn.QuerySingleOrDefaultAsync<Client>(
                    "SELECT TOP 1 [Id], [Name], [Group], [ConnectionId], [Ip], [Os], [CoreCount], [Memory], [IsConnected], [CreationTime], [LastModificationTime] FROM [Client] WHERE [Name] = @Name AND [Group] = @Group",
                    new {Name = name, Group = group});
                return client;
            }
        }

        public async Task ConnectClient(string name, string group, string connectionId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [Client] SET [IsConnected] = 'true', [ConnectionId] = @ConnectionId, [LastModificationTime] =  CURRENT_TIMESTAMP WHERE [Name] = @Name AND [Group] = @Group",
                    new {Name = name, Group = group, ConnectionId = connectionId});
            }
        }

        public async Task DisconnectClient(string name, string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [Client] SET [IsConnected] = 'false', [LastModificationTime] =  CURRENT_TIMESTAMP WHERE [Name] = @Name AND [Group] = @Group",
                    new {Name = name, Group = group});
            }
        }

        public async Task<IEnumerable<Client>> GetAvailableClients(string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return (await conn.QueryAsync<Client>(
                    @"SELECT [Id], [Name], [Group], [ConnectionId], [Ip], [Os], [CoreCount], [Memory], [IsConnected], [CreationTime], [LastModificationTime] FROM [Client]
 WHERE [Group] = @Group AND DATEDIFF(SECOND, [LastModificationTime], CURRENT_TIMESTAMP) < 6　AND [IsConnected] = 'true'",
                    new {Group = group}));
            }
        }

        public async Task DisconnectAllClients()
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync("UPDATE [Client] SET [IsConnected] = 'false';");
            }
        }

        #endregion

        #region Job

        public async Task<Job> GetJob(string name, string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                var job = await conn.QuerySingleOrDefaultAsync<Job>(
                    @"SELECT TOP 1 [Id], [Trigger], [Performer], [Executor], [Name], [Group], [SchedName], [SchedInstanceId], [Load], [Sharding],
                        [ShardingParameters], [Description], [Owner], [AllowConcurrent], [CreationTime], [LastModificationTime] FROM [Job] WHERE [Name] = @Name AND [Group] = @Group",
                    new {Name = name, Group = group});

                if (job != null)
                {
                    var properties = await conn.QueryAsync<JobProperty>(
                        "SELECT [Id], [JobId], [Name], [Value], [CreationTime] FROM [JobProperty] WHERE [JobId] = @Id",
                        new {Id = job.Id});
                    foreach (var property in properties)
                    {
                        if (!job.Properties.ContainsKey(property.Name))
                        {
                            job.Properties.Add(property.Name, property.Value);
                        }
                    }
                }

                return job;
            }
        }

        public async Task<Job> GetJob(string jobId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                var job = await conn.QuerySingleOrDefaultAsync<Job>(
                    @"SELECT TOP 1 [Id], [Trigger], [Performer], [Executor], [Name], [Group], [SchedName], [SchedInstanceId],[SchedName], [SchedInstanceId], [Load], [Sharding],
 [ShardingParameters], [Description], [Owner], [AllowConcurrent], [CreationTime], [LastModificationTime] FROM [Job] WHERE [Id] = @Id",
                    new {Id = jobId});
                if (job != null)
                {
                    var properties = await conn.QueryAsync<JobProperty>(
                        "SELECT [Id], [JobId], [Name], [Value], [CreationTime] FROM [JobProperty] WHERE [JobId] = @Id",
                        new {Id = jobId});
                    foreach (var property in properties)
                    {
                        if (!job.Properties.ContainsKey(property.Name))
                        {
                            job.Properties.Add(property.Name, property.Value);
                        }
                    }
                }

                return job;
            }
        }

        public async Task<string> AddJob(Job job)
        {
            job.Id = string.IsNullOrWhiteSpace(job.Id) ? Guid.NewGuid().ToString("N") : job.Id;
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO [Job] ([Id], [Trigger], [Performer], [Executor], [Name], [Group], [SchedName], [SchedInstanceId], [Load], [Sharding],
 [ShardingParameters], [Description], [Owner], [AllowConcurrent], [CreationTime]) VALUES (@Id, @Trigger, @Performer, @Executor, @Name, @Group, @SchedName, @SchedInstanceId, @Load, @Sharding,
@ShardingParameters, @Description, @Owner, @AllowConcurrent, CURRENT_TIMESTAMP)",
                        job, trans);
                    await conn.ExecuteAsync(
                        "INSERT INTO [JobProperty] ([JobId], [Name], [Value], [CreationTime]) VALUES (@JobId, @Name, @Value, CURRENT_TIMESTAMP)",
                        job.Properties.Select(p => new JobProperty {JobId = job.Id, Name = p.Key, Value = p.Value}),
                        trans);
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }

            return job.Id;
        }

        public async Task UpdateJob(Job job)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    await conn.ExecuteAsync(
                        @"UPDATE [Job] SET [Trigger] = @Trigger, [Performer] = @Performer, [Executor] = @Executor, [Name] = @Name,
 [Group] = @Group, [Node] = @Node, [Load] = @Load, [Sharding] = @Sharding, [ShardingParameters] = @ShardingParameters, [Description] = @Description, [Owner] = @Owner, [AllowConcurrent] = @AllowConcurrent, [LastModificationTime] = CURRENT_TIMESTAMP WHERE ID = @Id",
                        job, trans);

                    await conn.ExecuteAsync(
                        "DELETE FROM [JobProperty] WHERE [JobId] = @Id",
                        new {job.Id}, trans);

                    await conn.ExecuteAsync(
                        "INSERT INTO [JobProperty] ([JobId], [Name], [Value], [CreationTime]) VALUES (@JobId, @Name, @Value, CURRENT_TIMESTAMP)",
                        job.Properties.Select(p => new JobProperty {JobId = job.Id, Name = p.Key, Value = p.Value}),
                        trans);
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        public async Task DeleteJob(string jobId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    await conn.ExecuteAsync(
                        "DELETE FROM [JobProperty] WHERE [JobId] = @Id",
                        new {Id = jobId}, trans);
                    await conn.ExecuteAsync(
                        "DELETE FROM [Job] WHERE Id = @Id",
                        new {Id = jobId}, trans);
                    await conn.ExecuteAsync(
                        "DELETE FROM [JobState] WHERE [JobId] = @Id",
                        new {Id = jobId}, trans);
                    await conn.ExecuteAsync(
                        "DELETE FROM [Log] WHERE [JobId] = @Id",
                        new {Id = jobId}, trans);
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        #endregion

        #region  Node

        public async Task RegisterNode(Node node)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var exists = await conn.ExecuteAsync(
                                     "UPDATE [Node] SET [IsConnected] = 'true', [LastModificationTime] = CURRENT_TIMESTAMP WHERE [SchedInstanceId] = @SchedInstanceId AND [SchedName] = @SchedName",
                                     new {node.SchedInstanceId, node.SchedName}, trans) > 0;
                    if (!exists)
                    {
                        await conn.ExecuteAsync(
                            @"INSERT INTO [Node] ([SchedInstanceId], [SchedName], [Provider], [ConnectionString], [TriggerTimes], [IsConnected], [CreationTime], 
[LastModificationTime]) VALUES (@SchedInstanceId, @SchedName, @Provider, @ConnectionString, @TriggerTimes, @IsConnected, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)",
                            node, trans);
                    }

                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        public async Task DisconnectNode(string schedName, string schedInstanceId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [Node] SET [IsConnected] = 'false', [LastModificationTime] = CURRENT_TIMESTAMP WHERE [SchedInstanceId] = @SchedInstanceId AND [SchedName] = @SchedName",
                    new {SchedInstanceId = schedInstanceId, SchedName = schedName});
            }
        }

        public async Task<Node> GetMinimumTriggerTimesNode()
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<Node>(
                    $"SELECT TOP 1 [Id], [SchedName], [SchedInstanceId], [Provider], [TriggerTimes], [IsConnected], [ConnectionString], [CreationTime], [LastModificationTime] FROM [Node] WHERE [IsConnected] = 'true' AND DATEDIFF(SECOND, [LastModificationTime], CURRENT_TIMESTAMP) < {SwarmConsts.NodeOfflineInterval}  ORDER BY [TriggerTimes]");
            }
        }

        public async Task<Node> GetAvailableNode(string schedName, string schedInstanceId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<Node>(
                    "SELECT TOP 1 [Id], [SchedName], [SchedInstanceId], [Provider], [TriggerTimes], [IsConnected], [ConnectionString], [CreationTime], [LastModificationTime] FROM [Node] WHERE [IsConnected] = 'true' AND [SchedName] = @SchedName AND [SchedInstanceId] = @SchedInstanceId",
                    new {SchedName = schedName, SchedInstanceId = schedInstanceId});
            }
        }

        public async Task IncreaseTriggerTime(string schedName, string schedInstanceId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [Node] SET [TriggerTimes] = [TriggerTimes] + 1  WHERE [SchedInstanceId] = @SchedInstanceId AND [SchedName] = @SchedName",
                    new {SchedInstanceId = schedInstanceId, SchedName = schedName});
            }
        }

        public async Task ClientHeartbeat(string name, string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [Client] SET [LastModificationTime] = CURRENT_TIMESTAMP, [IsConnected] = 'true'  WHERE [Name] = @Name AND [Group] = @Group",
                    new {Name = name, Group = group});
            }
        }

        #endregion

        #region Log

        public async Task AddLog(Log log)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    @"INSERT [Log] ([ClientName], [ClientGroup], [JobId], [TraceId], [Sharding], [Msg], [CreationTime]) VALUES (@ClientName, @ClientGroup, @JobId, @TraceId, @Sharding, @Msg, CURRENT_TIMESTAMP);",
                    log);
            }
        }

        #endregion

        #region ClientProcess

        public async Task AddOrUpdateClientProcess(ClientProcess clientProcess)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.OpenAsync();
                var exists = await conn.ExecuteAsync(
                                 @"UPDATE [ClientProcess] SET [State] = @State, [Msg] = @Msg, [App] = @App, [AppArguments] = @AppArguments, [LastModificationTime] = CURRENT_TIMESTAMP WHERE
 [Name] = @Name AND [Group] = @Group AND [JobId] = @JobId AND [TraceId] = @TraceId AND [Sharding] = @Sharding AND [ProcessId] = @ProcessId",
                                 clientProcess) > 0;
                if (!exists)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO [ClientProcess] ([Name], [Group], [JobId], [TraceId], [Sharding], [ProcessId], [State], [App], 
[AppArguments], [Msg], [CreationTime], [LastModificationTime]) VALUES (@Name, @Group, @JobId, @TraceId, @Sharding, @ProcessId, @State, @App, @AppArguments, @Msg, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)",
                        clientProcess);
                }
            }
        }

        #endregion
    }
}