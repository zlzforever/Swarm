using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Swarm.Core.Common
{
    public static class DbInitializer
    {
        public static void Init(string[] args)
        {
            var config = new ConfigurationBuilder().AddCommandLine(args, new Dictionary<string, string>
                {
                    {"--i", "init"},
                    {"--d", "db"},
                    {"--r", "re-create"}
                }).AddJsonFile("appsettings.json")
                .Build();
            var init = config.GetValue<string>("init");
            if (string.IsNullOrWhiteSpace(init))
            {
                return;
            }

            if (!bool.Parse(init))
            {
                return;
            }

            var db = config.GetValue<string>("db");
            var r = config.GetValue<bool>("re-create");
            if (string.IsNullOrWhiteSpace(db))
            {
                Console.WriteLine("Supply target database like: --d sqlserver");
                return;
            }
            
            var connectionString = config.GetSection("Swarm").GetValue<string>("QuartzConnectionString");
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

            Environment.Exit(0);
        }
    }
}