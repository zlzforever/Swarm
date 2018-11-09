using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Swarm.Core.Common
{
    public static class DbInitializer
    {
        public static ILogger Logger;

        public static bool Init(string[] args)
        {
            var init = args.Contains("-i");
            if (!init)
            {
                Logger?.LogInformation("Ignore init database");
            }

            var configPath = args.First(a => !a.StartsWith("-"));
            ;
            var config = new ConfigurationBuilder().AddJsonFile(configPath)
                .Build();

            var swarm = config.GetSection("Swarm");
            var connectionString = swarm.GetValue<string>("QuartzConnectionString");
            var db = swarm.GetValue<string>("Provider");
            switch (db.ToLower())
            {
                case "sqlserver":
                {
                    new SqlServerMigrator(connectionString, args.Contains("-r"), Logger).Migrate();
                    break;
                }
                default:
                {
                    Console.WriteLine($"Unsupported provider: {db}");
                    return false;
                }
            }

            return true;
        }
    }
}