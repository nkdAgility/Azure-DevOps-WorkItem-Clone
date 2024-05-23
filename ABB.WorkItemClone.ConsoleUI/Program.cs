using Microsoft.Extensions.DependencyInjection;

using System;
using System.Reflection;
using Microsoft.Extensions.Hosting;

using Spectre.Console;
using Spectre.Console.Cli;
using ABB.WorkItemClone.ConsoleUI.Commands;

namespace ABB.WorkItemClone.ConsoleUI
{
    internal class Program
    {
        static int Main(string[] args)
        {
            AnsiConsole.Write(new FigletText("ABB WIT").LeftJustified().Color(Color.Red));
            AnsiConsole.MarkupLine($"[bold white]ABB Work Item Tools[/] [bold yellow]{GetVersionTextForLog()}[/]");

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.PropagateExceptions();
                config.AddCommand<WorkItemCloneCommand>("clone");
            });

            try
            {
                return app.Run(args);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                return -99;
            }
        }

        public static string GetVersionTextForLog()
        {
            Version runningVersion = GetRunningVersion();
            string debugVersion = (string.IsNullOrEmpty(ThisAssembly.Git.BaseTag) ? "v" + runningVersion + "-local" : ThisAssembly.Git.BaseTag + "-" + ThisAssembly.Git.Commits + "-local");
            string textVersion = ((runningVersion.Major > 0) ? "v" + runningVersion : debugVersion);
            return textVersion;
        }

        public static Version GetRunningVersion()
        {
            Version assver = Assembly.GetEntryAssembly()?.GetName().Version;
            if (assver == null)
            {
                return new Version("0.0.0");
            }
            return new Version(assver.Major, assver.Minor, assver.Build);
        }
    }
}
