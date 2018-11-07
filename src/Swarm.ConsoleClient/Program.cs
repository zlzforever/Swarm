using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Swarm.Client;

namespace Swarm.ConsoleClient
{
    class Program
    {
        private static readonly string _processIdPath;
        private static Stream _processStream;

        static Program()
        {
            _processIdPath = Path.Combine(AppContext.BaseDirectory, "processId");
        }

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: SerilogConsoleTheme.ConsoleTheme).WriteTo.RollingFile("swarm.log")
                .CreateLogger();

            if (CheckIfIsRunning())
            {
                Log.Logger.Error("Client is running.");
                return;
            }

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
            client.Start().ConfigureAwait(false);
            Console.WriteLine("Press any key to exit:");
            Console.Read();
            client.Stop();
            Dispose();
            Console.WriteLine("Exited.");
        }

        private static bool CheckIfIsRunning()
        {
            try
            {
                _processStream = File.Open(_processIdPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static void Dispose()
        {
            _processStream?.Dispose();
        }
    }
}