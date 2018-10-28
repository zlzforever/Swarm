using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Swarm.Migrator.Sql;

namespace Swarm.Migrator
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddCommandLine(args, new Dictionary<string, string>
                {
                    {"--d", "db"},
                    {"--r", "re-create"}
                }).AddJsonFile("appsettings.json")
                .Build();
            var db = config.GetValue<string>("db");
            var r = config.GetValue<bool>("re-create");
            if (string.IsNullOrWhiteSpace(db))
            {
                Console.WriteLine("Supply target database like: --d sqlserver");
                return;
            }

            var connectionString = config.GetConnectionString("DefaultConnection");
            switch (db.ToLower())
            {
                case "sqlserver":
                {
                    var migrator = new SqlServerMigrator(connectionString, r);
                    migrator.Migrate();
                    break;
                }
                default:
                {
                    Console.WriteLine("Supply correct target database like: --d sqlserver");
                    break;
                }
            }
        }
    }
}