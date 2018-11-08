using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Dapper;

namespace Swarm.Core.Common
{
    public class SqlServerMigrator : IMigration
    {
        private readonly string _connectionString;
        private readonly bool _reCreate;

        public SqlServerMigrator(string connectionString, bool reCreate)
        {
            _connectionString = connectionString;
            _reCreate = reCreate;
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
                    }
                    else
                    {
                        Console.WriteLine("Database already exists");
                        return;
                    }
                }

                Console.WriteLine("Try create database: " + db);
                masterConn.Execute($"CREATE DATABASE {db}");

                ExecuteSql();
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