using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Swarm.Core.Common
{
    public class SqlServerMigrator : IMigration
    {
        private readonly string _connectionString;
        private readonly bool _reCreate;
        private readonly ILogger _logger;

        public SqlServerMigrator(string connectionString, bool reCreate,ILogger logger=null)
        {
            _connectionString = connectionString;
            _reCreate = reCreate;
            _logger = logger;
        }

        public void Migrate()
        {
            var start = _connectionString.IndexOf("Initial Catalog=", StringComparison.Ordinal);
            var end = _connectionString.IndexOf(";", start, StringComparison.Ordinal);
            var ic = _connectionString.Substring(start, end - start);
            using (var masterConn =
                new SqlConnection(_connectionString.Replace(ic, "Initial Catalog=master")))
            {
                var conn = new SqlConnection(_connectionString);
                var db = conn.Database;
                conn.Close();
                conn.Dispose();

                var exists = masterConn.QuerySingle<int>(
                                 $"select count(*) from sys.databases where name = '{db}'") > 0;
                if (exists)
                {
                    if (_reCreate)
                    {
                        masterConn.Execute($"DROP DATABASE {db}");
                        _logger?.LogInformation($"Drop database: {db} success");
                    }
                    else
                    {
                        _logger?.LogInformation("Database already exists, ignore Quartz database init");
                        return;
                    }
                }

                masterConn.Execute($"CREATE DATABASE {db}");
                _logger?.LogInformation($"Create database: {db} success");

                ExecuteSql();

                _logger?.LogInformation("Init tables success");
            }
        }

        private void ExecuteSql()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var reader =
                    new StreamReader(GetType().Assembly.GetManifestResourceStream("Swarm.Core.Sql.sqlserver.sql") ??
                                     throw new Exception("Sql resource unfounded")))
                {
                    var sqls = reader.ReadToEnd();

                    var start = _connectionString.IndexOf("Initial Catalog=", StringComparison.Ordinal);
                    var end = _connectionString.IndexOf(";", start, StringComparison.Ordinal);
                    var ic = _connectionString.Substring(start, end - start);
                    {
                        foreach (var sql in sqls.Split(new[] {"GO"}, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!string.IsNullOrWhiteSpace(sql))
                            {
                                conn.Execute(sql);
                            }
                        }
                    }
                }
            }
        }
    }
}