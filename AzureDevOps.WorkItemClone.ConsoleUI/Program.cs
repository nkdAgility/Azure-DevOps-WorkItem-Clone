using Microsoft.Extensions.DependencyInjection;

using System;
using System.Reflection;
using Microsoft.Extensions.Hosting;

using Spectre.Console;
using Spectre.Console.Cli;
using AzureDevOps.WorkItemClone.ConsoleUI.Commands;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;

namespace ABB.WorkItemClone.ConsoleUI
{
    internal class Program
    {
        private const string ConnectionString = "InstrumentationKey=6b3339ba-05e1-447e-ab55-32c34cf9998e;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/;ApplicationId=0e7b1578-b095-423f-9644-63dbd081d493";

        static async Task<int> Main(string[] args)
        {

                https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel

            // Create a new tracer provider builder and add an Azure Monitor trace exporter to the tracer provider builder.
            // It is important to keep the TracerProvider instance active throughout the process lifetime.
            // See https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace#tracerprovider-management
            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddAzureMonitorTraceExporter(o => o.ConnectionString = ConnectionString);

            // Add an Azure Monitor metric exporter to the metrics provider builder.
            // It is important to keep the MetricsProvider instance active throughout the process lifetime.
            // See https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/metrics#meterprovider-management
            var metricsProvider = Sdk.CreateMeterProviderBuilder()
                .AddRuntimeInstrumentation()
                .AddAzureMonitorMetricExporter(o => o.ConnectionString = ConnectionString);
            // Create a new logger factory.
            // It is important to keep the LoggerFactory instance active throughout the process lifetime.
            // See https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/logs#logger-management
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.AddAzureMonitorLogExporter(o => o.ConnectionString = ConnectionString);
                });
            });


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
