using System;
using System.Collections.Generic;
using Serilog.Sinks.SystemConsole.Themes;

namespace Swarm.Node
{
   public static class SerilogConsoleTheme
    {
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