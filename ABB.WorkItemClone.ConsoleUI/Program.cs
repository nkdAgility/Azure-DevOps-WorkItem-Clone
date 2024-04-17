using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.Reflection;

namespace ABB.WorkItemClone.ConsoleUI
{
    internal class Program
    {
        static void Main(string[] args)
        {
           // var builder = Host.CreateApplicationBuilder(args);
           // builder.Logging.ClearProviders();
           // string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] [" + GetVersionTextForLog() + "] {Message:lj}{NewLine}{Exception}";
           // builder.Services.AddSerilog(lc => lc
           //         .WriteTo.SpectreConsole(theme: AnsiConsoleTheme.Code, outputTemplate: outputTemplate)
           //         .Enrich.FromLogContext()
           //         .Enrich.WithMachineName()
           //         );
           // // Setup
           //// builder.Services.AddKeyedSingleton<IWorkEnvironmentTarget, WorkEnvironemntTarget>("AzureDevOps");

           // // Add Hosted Services
           //// var weo1 = new WorkEnvironmentOptions("WE1", 4, "AzureDevOps");
           //// weo1.TeamMembers.Clear();
           //// weo1.TeamMembers.Add(new TeamMember("Product Owner", "New"));
           //// weo1.TeamMembers.Add(new TeamMember("Team Member 1", "Approved"));
           // //weo1.TeamMembers.Add(new TeamMember("Team Member 2", "Committed"));

           // //builder.Services.AddSingleton<IHostedService, WorkEnvironment>(serviceProvider => new WorkEnvironment(weo1, serviceProvider, serviceProvider.GetService<ILogger<WorkEnvironment>>()));

           // //Build Host
           // var host = builder.Build();
           // host.Run();
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
