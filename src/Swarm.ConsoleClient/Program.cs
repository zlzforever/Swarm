using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Swarm.Client;

namespace Swarm.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: SerilogConsoleTheme.ConsoleTheme).WriteTo.RollingFile("swarm.log")
                .CreateLogger();

            IServiceCollection services = new ServiceCollection();
            services.AddLogging(options => { options.AddSerilog(); });

            string file;
            if (args.Length == 1)
            {
                file = args[0];
                if (!File.Exists(file))
                {
                    Log.Logger.Error($"File not exists: {file}");
                    return;
                }
            }
            else if (args.Length > 1)
            {
                Log.Logger.Error("Use command: Swarm.ConsoleClient {file}");
                return;
            }
            else
            {
                file = "config.ini";
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddIniFile(file)
                .Build();
            services.AddSwarmClient(config.GetSection("Client"));
            var client = services.BuildServiceProvider().GetRequiredService<ISwarmClient>();
            client.Start();
            Console.WriteLine("Press any key to exit:");
            Console.Read();
            client.Stop();
            Console.WriteLine("Exited.");
        }
    }
}