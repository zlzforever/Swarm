using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Swarm.Client;

namespace Swarm.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console().WriteTo.RollingFile("swarmclient.log")
                .CreateLogger();

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

            //Microsoft.Extensions.Hosting
            var host = new HostBuilder()
                .ConfigureHostConfiguration(builder => { builder.AddIniFile(file, true); })
                .ConfigureAppConfiguration((context, builder) =>
                {
                    context.HostingEnvironment.ApplicationName = "SwarmConsoleClient";
                    builder.SetBasePath(AppContext.BaseDirectory);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSwarmClient(context.Configuration.GetSection("Client"));
                    services.AddHostedService<SwarmClientHostService>();
                })
                .ConfigureLogging((logging) => { logging.AddSerilog(); })
                .UseConsoleLifetime()
                .Build();

            host.Run();
        }
    }
}