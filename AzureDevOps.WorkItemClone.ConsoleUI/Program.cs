using Microsoft.Extensions.DependencyInjection;

using System;
using System.Reflection;
using Microsoft.Extensions.Hosting;

using Spectre.Console;
using Spectre.Console.Cli;
using AzureDevOps.WorkItemClone.ConsoleUI.Commands;
using System.Text;
using System.Threading.Tasks;
using AzureDevOps.WorkItemClone;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ABB.WorkItemClone.ConsoleUI
{
    internal class Program
    {
      

        static async Task<int> Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            var runningVersion = GetRunningVersion();

            Telemetry.Initialize(runningVersion.versionString);
            AnsiConsole.Write(new FigletText("Azure DevOps").LeftJustified().Color(Color.Red));
            AnsiConsole.Write(new FigletText("Work Item Clone").LeftJustified().Color(Color.Red));
            AnsiConsole.MarkupLine($"[bold white]Azure DevOps Work Item Clone[/] [bold yellow]{runningVersion.versionString}[/]");

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.PropagateExceptions();
                config.AddCommand<WorkItemCloneCommand>("clone");
                config.AddCommand<WorkItemInitCommand>("init");
            });

            try
            {
                return await app.RunAsync(args);
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                return -99;
            }
            Console.WriteLine("finished");
        }


        public static (Version version, string PreReleaseLabel, string versionString) GetRunningVersion()
        {
            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()?.Location);
            var matches = Regex.Matches(myFileVersionInfo.ProductVersion, @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<build>0|[1-9]\d*)(?:-((?<label>:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<fullEnd>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$");
            Version version = new Version(myFileVersionInfo.FileVersion);
            string textVersion = "v" + version.Major + "." + version.Minor + "." + version.Build + "-" + matches[0].Groups[1].Value;
            return (version, matches[0].Groups[1].Value, textVersion);
        }
    }
}
