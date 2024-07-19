using AzureDevOps.WorkItemClone;
using AzureDevOps.WorkItemClone.DataContracts;
using AzureDevOps.WorkItemClone.ConsoleUI.DataContracts;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq;
using Microsoft.SqlServer.Server;

namespace AzureDevOps.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemInitCommand : WorkItemCommandBase<WorkItemCloneCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, WorkItemCloneCommandSettings settings)
        {
            if (!FileStoreCheckExtensionMatchesFormat(settings.configFile, settings.ConfigFormat))
            {
                AnsiConsole.MarkupLine($"[bold red]The file extension of {settings.configFile} does not match the format {settings.ConfigFormat.ToString()} selected! Please rerun with the correct format You can use --configFormat JSON or update your file to YAML[/]");
                return -1;
            }
            var configFile = EnsureConfigFileAskIfMissing(settings.configFile);
            WorkItemCloneCommandSettings config = null;
            if (FileStoreExist(configFile, settings.ConfigFormat))
            {
                var proceedWithSettings = AnsiConsole.Prompt(
                new SelectionPrompt<bool> { Converter = value => value ? "Yes" : "No" }
                    .Title("The config file name used exists would you like to load this one?")
                    .AddChoices(true, false));
                if (proceedWithSettings)
                {
                    config = FileStoreLoad<WorkItemCloneCommandSettings>(configFile, settings.ConfigFormat);
                }
            }
            if (config == null)
            {
                config = new WorkItemCloneCommandSettings();
            }
            CombineValuesFromConfigAndSettings(settings, config);

            WriteOutSettings(config);

            FileStoreSave(configFile, config, settings.ConfigFormat);

            AnsiConsole.WriteLine($"Settings saved to {configFile}!");

            return 0;
        }
    }
}
