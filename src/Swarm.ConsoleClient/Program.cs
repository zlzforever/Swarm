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
                .WriteTo.Console(theme: ConsoleTheme).WriteTo.RollingFile("swarm.log")
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
                file = "/Users/lewis/swarm.ini";
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

        public static SystemConsoleTheme ConsoleTheme { get; set; } = new SystemConsoleTheme(
            new Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle>
            {
                [ConsoleThemeStyle.Text] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Black
                },
                [ConsoleThemeStyle.SecondaryText] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Black
                },
                [ConsoleThemeStyle.TertiaryText] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Black
                },
                [ConsoleThemeStyle.Invalid] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Yellow
                },
                [ConsoleThemeStyle.Null] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White
                },
                [ConsoleThemeStyle.Name] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Black
                },
                [ConsoleThemeStyle.String] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Black
                },
                [ConsoleThemeStyle.Number] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Black
                },
                [ConsoleThemeStyle.Boolean] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Black
                },
                [ConsoleThemeStyle.Scalar] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Black
                },
                [ConsoleThemeStyle.LevelVerbose] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Gray,
                    Background = ConsoleColor.DarkGray
                },
                [ConsoleThemeStyle.LevelDebug] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.DarkGray
                },
                [ConsoleThemeStyle.LevelInformation] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Blue
                },
                [ConsoleThemeStyle.LevelWarning] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.DarkGray,
                    Background = ConsoleColor.Yellow
                },
                [ConsoleThemeStyle.LevelError] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Red
                },
                [ConsoleThemeStyle.LevelFatal] = new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Red
                }
            });
    }
}