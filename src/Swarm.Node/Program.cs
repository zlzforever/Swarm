using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Swarm.Core.Common;

namespace Swarm.Node
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string configPath;
            var argList = new List<string>(args);
            var f = argList.FirstOrDefault(a => !a.StartsWith("-"));
            if (f != null)
            {
                if (File.Exists(f))
                {
                    configPath = new FileInfo(f).FullName;
                }
                else
                {
                    Console.WriteLine("Usage: Swarm.Node [path-to-configuration] [options]");
                    Console.WriteLine($"ERROR: File {args[0]} NOT exits");
                    return;
                }
            }
            else
            {
                configPath = "appsettings.json";
                argList.Add(configPath);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console().WriteTo.RollingFile("swarm.log")
                .CreateLogger();

            DbInitializer.Logger = new SerilogLoggerFactory().CreateLogger("Swarm.Node");
            var internalArgs = argList.ToArray();
            if (!DbInitializer.Init(internalArgs))
            {
                return;
            }


            Log.Logger.Information($"Load configuration from {configPath}");
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configPath, true)
                .Build();

            new SwarmDbContext().CreateDbContext(new[] {configPath}).Database.Migrate();
            Log.Logger.Information($"Swarm database update success");
            var host = CreateWebHostBuilder(args).UseConfiguration(config).Build();
            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>().UseSerilog();

        public static SystemConsoleTheme ConsoleLogTheme { get; set; } = new SystemConsoleTheme(
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