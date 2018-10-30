using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic;
using Swarm.Basic.Entity;

namespace Swarm.Core.Impl
{
    public class SqlServerSwarmStore : ISwarmStore
    {
        private readonly SwarmOptions _options;
        private readonly ILogger _logger;

        public SqlServerSwarmStore(IOptions<SwarmOptions> options, ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<SqlServerSwarmStore>();
        }

        #region Client

        public async Task<bool> AddClient(Client client)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.ExecuteAsync(
                           "INSERT INTO [SWARM_CLIENTS] ([NAME], [GROUP], [CONNECTION_ID], [IP], [IS_CONNECTED], [CREATION_TIME], [LAST_MODIFICATION_TIME]) VALUES (@Name, @Group, @ConnectionId, @Ip, @IsConnected, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)",
                           client) > 0;
            }
        }

        public async Task RemoveClient(int clientId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "DELETE FROM [SWARM_CLIENTS] WHERE [ID] = @Id",
                    new {Id = clientId});
            }
        }

        public async Task<Client> GetClient(string name, string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                var client = await conn.QuerySingleOrDefaultAsync<Client>(
                    "SELECT TOP 1 [ID], [NAME], [GROUP], [CONNECTION_ID] AS CONNECTIONID, [IP], [IS_CONNECTED] AS ISCONNECTED, [CREATION_TIME] AS CREATIONTIME, [LAST_MODIFICATION_TIME] AS LASTMODIFICATIONTIME FROM [SWARM_CLIENTS] WHERE [NAME] = @Name AND [GROUP] = @Group",
                    new {Name = name, Group = group});
                return client;
            }
        }

        public async Task ConnectClient(string name, string group, string connectionId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [SWARM_CLIENTS] SET [IS_CONNECTED] = 'true', [CONNECTION_ID] = @ConnectionId, [LAST_MODIFICATION_TIME] =  CURRENT_TIMESTAMP WHERE [NAME] = @Name AND [GROUP] = @Group",
                    new {Name = name, Group = group, ConnectionId = connectionId});
            }
        }

        public async Task DisconnectClient(string name, string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [SWARM_CLIENTS] SET [IS_CONNECTED] = 'false', [LAST_MODIFICATION_TIME] =  CURRENT_TIMESTAMP WHERE [NAME] = @Name AND [GROUP] = @Group",
                    new {Name = name, Group = group});
            }
        }

        public async Task<IEnumerable<Client>> GetClients(string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return (await conn.QueryAsync<Client>(
                    "SELECT [ID], [NAME], [IP], [CONNECTION_ID] AS CONNECTIONID, [GROUP], [IS_CONNECTED] AS ISCONNECTED FROM [SWARM_CLIENTS] WHERE [GROUP] = @Group",
                    new {Group = group}));
            }
        }

        public async Task DisconnectAllClients()
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync("UPDATE [SWARM_CLIENTS] SET [IS_CONNECTED] = 'false';");
            }
        }

        #endregion

        public async Task<bool> CheckJobExists(string id)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.QuerySingleAsync<int>(
                           "SELECT COUNT(*) FROM [SWARM_JOBS] WHERE [ID] = @Id",
                           new {Id = id}) > 0;
            }
        }

        public async Task<bool> IsJobExists(string name, string group)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.QuerySingleAsync<int>(
                           "SELECT COUNT(*) FROM [SWARM_JOBS] WHERE [NAME] = @Name AND [GROUP] = @Group",
                           new {Name = name, Group = group}) > 0;
            }
        }

        public async Task<string> AddJob(Job job)
        {
            job.Id = Guid.NewGuid().ToString("N");
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO [SWARM_JOBS] ([ID], [STATE], [TRIGGER], [PERFORMER], [EXECUTOR], [NAME], [GROUP], [NODE], [LOAD], [SHARDING],
 [SHARDING_PARAMETERS], [DESCRIPTION], [RETRY_COUNT], [OWNER], [CONCURRENT_EXECUTION_DISALLOWED], [CREATION_TIME]) VALUES (@Id, @State, @Trigger, @Performer, @Executor, @Name, @Group, @Node, @Load, @Sharding,
@ShardingParameters, @Description, @RetryCount, @Owner, @ConcurrentExecutionDisallowed, CURRENT_TIMESTAMP)",
                        job, trans);
                    await conn.ExecuteAsync(
                        "INSERT INTO [SWARM_JOB_PROPERTIES] ([JOB_ID], [NAME], [VALUE], [CREATION_TIME]) VALUES (@JobId, @Name, @Value, CURRENT_TIMESTAMP)",
                        job.Properties.Select(p => new JobProperty {JobId = job.Id, Name = p.Key, Value = p.Value}),
                        trans);
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Save job failed: {ex.Message}.");
                    trans.Rollback();
                    job.Id = null;
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
                        @"UPDATE [SWARM_JOBS] SET [STATE] = @State, [TRIGGER] = @Trigger, [PERFORMER] = @Performer, [EXECUTOR] = @Executor, [NAME] = @Name,
 [GROUP] = @Group, [NODE] = @Node, [LOAD] = @Load, [SHARDING] = @Sharding, [SHARDING_PARAMETERS] = @ShardingParameters, [DESCRIPTION] = @Description, [RETRY_COUNT] = @RetryCount,
 [OWNER] = @Owner, [CONCURRENT_EXECUTION_DISALLOWED] = @ConcurrentExecutionDisallowed, [LAST_MODIFICATION_TIME] = CURRENT_TIMESTAMP WHERE ID = @Id",
                        job, trans);

                    await conn.ExecuteAsync(
                        "DELETE FROM [SWARM_JOB_PROPERTIES] WHERE [JOB_ID] = @Id",
                        new {job.Id}, trans);

                    await conn.ExecuteAsync(
                        "INSERT INTO [SWARM_JOB_PROPERTIES] ([JOB_ID], [NAME], [VALUE], [CREATION_TIME]) VALUES (@JobId,@Name,@Value,CURRENT_TIMESTAMP)",
                        job.Properties.Select(p => new JobProperty {JobId = job.Id, Name = p.Key, Value = p.Value}),
                        trans);
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Update job failed: {ex.Message}.");
                    trans.Rollback();
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
                        "DELETE FROM [SWARM_JOB_PROPERTIES] WHERE [JOB_ID] = @Id",
                        new {Id = id}, trans);
                    await conn.ExecuteAsync(
                        "DELETE FROM [SWARM_JOBS] WHERE Id = @Id",
                        new {Id = id}, trans);
                    await conn.ExecuteAsync(
                        "DELETE FROM [SWARM_JOB_STATE] WHERE [JOB_ID] = @Id",
                        new {Id = id}, trans);
                    await conn.ExecuteAsync(
                        "DELETE FROM [SWARM_LOGS] WHERE [JOB_ID] = @Id",
                        new {Id = id}, trans);
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Add job failed: {ex.Message}.");
                    trans.Rollback();
                }
            }
        }

        public async Task<Job> GetJob(string id)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                var job = await conn.QuerySingleOrDefaultAsync<Job>(
                    @"SELECT [ID], [STATE], [TRIGGER], [PERFORMER] AS Performer, [EXECUTOR], [NAME], [GROUP], [NODE], [LOAD], [SHARDING],
 [SHARDING_PARAMETERS] AS SHARDINGPARAMETERS, [DESCRIPTION], [RETRY_COUNT] AS RETRYCOUNT, [OWNER], [CONCURRENT_EXECUTION_DISALLOWED] AS CONCURRENTEXECUTIONDISALLOWED, [CREATION_TIME] AS CREATIONTIME, [LAST_MODIFICATION_TIME] AS LASTMODIFICATIONTIME FROM [SWARM_JOBS] WHERE ID = @Id",
                    new {Id = id});
                if (job != null)
                {
                    var properties = await conn.QueryAsync<JobProperty>(
                        "SELECT [ID], [JOB_ID] AS JOBID, [NAME], [VALUE], [CREATION_TIME] AS CREATIONTIME FROM [SWARM_JOB_PROPERTIES] WHERE [JOB_ID] = @Id",
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

        public async Task<bool> IsJobExited(string jobId)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.QuerySingleAsync<int>(
                           "SELECT COUNT(*) FROM [SWARM_JOB_STATE] WHERE [JOB_ID] = @JobId AND [STATE] != @State",
                           new {JobId = jobId, State = State.Exit}) == 0;
            }
        }
        
        public async Task ChangeJobState(string jobId, State state)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [SWARM_JOBS] SET [STATE] = @State, [LAST_MODIFICATION_TIME] = CURRENT_TIMESTAMP WHERE [ID] = @Id AND [STATE] != @State",
                    new {Id = jobId, State = state});
            }
        }

        public async Task AddJobState(JobState jobState)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    @"INSERT [SWARM_JOB_STATE] ([JOB_ID], [TRACE_ID], [STATE], [CLIENT], [SHARDING], [MSG], [CREATION_TIME], [LAST_MODIFICATION_TIME]) VALUES (
@JobId, @TraceId, @State, @Client, @Sharding, @Msg, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);",
                    jobState);
            }
        }

        public async Task UpdateJobState(JobState jobState)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    "UPDATE [SWARM_JOB_STATE] SET [JOB_ID] = @JobId, [TRACE_ID] = @TraceId, [STATE] = @State, [CLIENT] = @Client, [SHARDING] = @Sharding, [MSG] = @Msg, [LAST_MODIFICATION_TIME] = CURRENT_TIMESTAMP WHERE [ID] = @Id;",
                    jobState);
            }
        }

        public async Task<JobState> GetJobState(string traceId, string client, int sharding)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<JobState>(
                    "SELECT [ID], [JOB_ID], [TRACE_ID], [STATE], [CLIENT], [SHARDING], [MSG], [CREATION_TIME], [LAST_MODIFICATION_TIME] FROM [SWARM_JOB_STATE] WHERE [TRACE_ID] = @TraceId AND [CLIENT] = @Client AND [Sharding] = @Sharding;",
                    new {TraceId = traceId, Client = client, Sharding = sharding});
            }
        }

        public async Task AddLog(Log log)
        {
            using (var conn = new SqlConnection(_options.ConnectionString))
            {
                await conn.ExecuteAsync(
                    @"INSERT [SWARM_LOGS] ([JOB_ID], [TRACE_ID], [MSG], [CREATION_TIME]) VALUES (@JobId, @TraceId, @Msg, CURRENT_TIMESTAMP);",
                    log);
            }
        }

    }
}