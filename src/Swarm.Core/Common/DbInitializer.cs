using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Swarm.Core.Common
{
    public static class DbInitializer
    {
        public static string Init(string[] args)
        {
            var init = args.Contains("-i");
            if (!init)
            {
                return "Ignore init database";
            }

            var configPath = args.First(a => !a.StartsWith("-"));;
            var config = new ConfigurationBuilder().AddJsonFile(configPath)
                .Build();

            var swarm = config.GetSection("Swarm");
            var connectionString = swarm.GetValue<string>("QuartzConnectionString");
            var db = swarm.GetValue<string>("Provider");
            switch (db.ToLower())
            {
                case "sqlserver":
                {
                    new SqlServerMigrator(connectionString, args.Contains("-r")).Migrate();
                    break;
                }
                default:
                {
                    Console.WriteLine($"Unsupported provider: {db}");
                    Environment.Exit(-1);
                    break;
                }
            }

            return "OK";
        }
    }
}