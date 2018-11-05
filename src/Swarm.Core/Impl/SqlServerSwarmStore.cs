using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
                    @"SELECT [Id], [Name], [Group], [ConnectionId], [Ip], [Os], [CoreCount], [Memory], [UserId], [IsConnected], [CreationTime], [LastModificationTime] FROM [Client]
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

        public async Task<Job> GetJob(string name, string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.QuerySingleAsync<Job>(
                    @"SELECT [Id], [UserId], [Trigger], [Performer], [Executor], [Name], [Group], [NodeId], [Load], [Sharding],
                        [ShardingParameters], [Description], [Owner], [AllowConcurrent], [CreationTime], [LastModificationTime] FROM [Job] WHERE [Name] = @Name AND [Group] = @Group",
                    new {Name = name, Group = group});
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
                        @"INSERT INTO [Job] ([Id], [Trigger], [Performer], [Executor], [Name], [Group], [UserId], [NodeId], [Load], [Sharding],
 [ShardingParameters], [Description], [Owner], [AllowConcurrent], [CreationTime]) VALUES (@Id, @Trigger, @Performer, @Executor, @Name, @Group, @UserId, @NodeId, @Load, @Sharding,
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

        public async Task DeleteJob(string id)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    await conn.ExecuteAsync(
                        "DELETE FROM [JobProperty] WHERE [JobId] = @Id",
                        new {Id = id}, trans);
                    await conn.ExecuteAsync(
                        "DELETE FROM [Job] WHERE Id = @Id",
                        new {Id = id}, trans);
                    await conn.ExecuteAsync(
                        "DELETE FROM [JobState] WHERE [JobId] = @Id",
                        new {Id = id}, trans);
                    await conn.ExecuteAsync(
                        "DELETE FROM [Log] WHERE [JobId] = @Id",
                        new {Id = id}, trans);
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        public async Task<Job> GetJob(string id)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                var job = await conn.QuerySingleOrDefaultAsync<Job>(
                    @"SELECT [Id], [UserId], [Trigger], [Performer], [Executor], [Name], [Group], [NodeId], [Load], [Sharding],
 [ShardingParameters], [Description], [Owner], [AllowConcurrent], [CreationTime], [LastModificationTime] FROM [Job] WHERE ID = @Id",
                    new {Id = id});
                if (job != null)
                {
                    var properties = await conn.QueryAsync<JobProperty>(
                        "SELECT [Id], [JobId], [Name], [Value], [CreationTime] FROM [JobProperty] WHERE [JobId] = @Id",
                        new {Id = id});
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

        public async Task AddJobState(JobState jobState)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    @"INSERT [JobState] ([JobId], [TraceId], [State], [Client], [Sharding], [Msg], [CreationTime], [LastModificationTime]) VALUES (
@JobId, @TraceId, @State, @Client, @Sharding, @Msg, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);",
                    jobState);
            }
        }

        public async Task UpdateJobState(JobState jobState)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [JobState] SET [JobId] = @JobId, [TraceId] = @TraceId, [State] = @State, [Client] = @Client, [Sharding] = @Sharding, [Msg] = @Msg, [LastModificationTime] = CURRENT_TIMESTAMP WHERE [Id] = @Id;",
                    jobState);
            }
        }

        public async Task RegisterNode(Node node)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var exists = await conn.ExecuteAsync(
                                     "UPDATE [Node] SET [LastModificationTime] = CURRENT_TIMESTAMP WHERE [SchedInstanceId] = @SchedInstanceId AND [SchedName] = @SchedName",
                                     new {node.SchedInstanceId, node.SchedName}, trans) > 0;
                    if (!exists)
                    {
                        await conn.ExecuteAsync(
                            @"INSERT INTO [Node] ([SchedInstanceId], [SchedName], [Provider], [ConnectionString], [TriggerTimes], [CreationTime], 
[LastModificationTime]) VALUES (@SchedInstanceId, @SchedName, @Provider, @ConnectionString, @TriggerTimes, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)",
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

        public async Task<Node> GetMinimumTriggerTimesNode()
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<Node>(
                    "SELECT TOP 1 [Id], [NodeId], [SchedName], [Provider], [TriggerTimes], [ConnectionString] FROM [Node] WHERE DATEDIFF(SECOND, [LastModificationTime], CURRENT_TIMESTAMP) < 6  ORDER BY [TriggerTimes]");
            }
        }

        public async Task<Node> GetNode(string nodeId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<Node>(
                    "SELECT TOP 1 [Id], [NodeId], [SchedName], [Provider], [TriggerTimes], [ConnectionString] FROM [Node] WHERE [NodeId] = @Id",
                    new {Id = nodeId});
            }
        }

        public async Task IncreaseTriggerTime(string name, string NodeId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [Node] SET [TriggerTimes] = [TriggerTimes] + 1  WHERE [NodeId] = @NodeId AND [SchedName] = @SchedName",
                    new {NodeId = NodeId, SchedName = name});
            }
        }

        public async Task ClientHeartbeat(string name, string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [Client] SET [LastModificationTime] = CURRENT_TIMESTAMP  WHERE [Name] = @Name AND [Group] = @Group",
                    new {Name = name, Group = group});
            }
        }

        public async Task<JobState> GetJobState(string traceId, string client, int Sharding)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<JobState>(
                    "SELECT [Id], [JobId], [TraceId], [State], [Client], [Sharding], [Msg], [CreationTime], [LastModificationTime] FROM [JobState] WHERE [TraceId] = @TraceId AND [Client] = @Client AND [Sharding] = @Sharding;",
                    new {TraceId = traceId, Client = client, Sharding = Sharding});
            }
        }

        public async Task AddLog(Log log)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    @"INSERT [Log] ([JobId], [TraceId], [Msg], [CreationTime]) VALUES (@JobId, @TraceId, @Msg, CURRENT_TIMESTAMP);",
                    log);
            }
        }
    }
}