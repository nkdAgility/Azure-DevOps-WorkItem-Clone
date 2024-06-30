using Microsoft.Extensions.DependencyInjection;

using System;
using System.Reflection;
using Microsoft.Extensions.Hosting;

using Spectre.Console;
using Spectre.Console.Cli;
using ABB.WorkItemClone.ConsoleUI.Commands;
using System.Text;
using System.Threading.Tasks;

namespace ABB.WorkItemClone.ConsoleUI
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            AnsiConsole.Write(new FigletText("Azure DevOps").LeftJustified().Color(Color.Red));
            AnsiConsole.Write(new FigletText("Work Item Clone").LeftJustified().Color(Color.Red));
            AnsiConsole.MarkupLine($"[bold white]Azure DevOps Work Item Clone[/] [bold yellow]{GetVersionTextForLog()}[/]");

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.PropagateExceptions();
                config.AddCommand<WorkItemCloneCommand>("clone");
#if DEBUG

                config.AddCommand<WorkItemCloneCommand>("init");
#endif
            });

            try
            {
                return await app.RunAsync(args);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                return -99;
            }
            Console.WriteLine("finished");
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
