using AzureDevOps.WorkItemClone;
using AzureDevOps.WorkItemClone.DataContracts;
using AzureDevOps.WorkItemClone.ConsoleUI.DataContracts;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq;

namespace AzureDevOps.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemInitCommand : WorkItemCommandBase<WorkItemCloneCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, WorkItemCloneCommandSettings settings)
        {

            var configFile = EnsureConfigFileAskIfMissing(settings.configFile);
            WorkItemCloneCommandSettings config = null;
            if (File.Exists(configFile))
            {
                var proceedWithSettings = AnsiConsole.Prompt(
                new SelectionPrompt<bool> { Converter = value => value ? "Yes" : "No" }
                    .Title("The config file name used exists would you like to load this one?")
                    .AddChoices(true, false));
                if (proceedWithSettings)
                {
                    config = LoadWorkItemCloneCommandSettingsFromFile(configFile);
                }
            }
            if (config == null)
            {
                config = new WorkItemCloneCommandSettings();
            }
            CombineValuesFromConfigAndSettings(settings, config);

            WriteOutSettings(config);




            System.IO.File.WriteAllText(configFile, JsonConvert.SerializeObject(config, Formatting.Indented));

            AnsiConsole.WriteLine("Settings saved to {configFile}!");

            return 0;
        }
    }
}
