using ABB.WorkItemClone.AzureDevOps;
using ABB.WorkItemClone.AzureDevOps.DataContracts;
using ABB.WorkItemClone.ConsoleUI.DataContracts;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemInitCommand : WorkItemCommandBase<WorkItemCloneCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, WorkItemCloneCommandSettings settings)
        {

            var configFile = EnsureConfigFileAskIfMissing(settings.configFile);

            if (File.Exists(configFile))
            {
                var proceedWithSettings = AnsiConsole.Prompt(
                new SelectionPrompt<bool> { Converter = value => value ? "Yes" : "No" }
                    .Title("The config file name used exists would you like to load this one?")
                    .AddChoices(true, false));
            }




            ConfigurationSettings configSettings = LoadConfigFile(settings.configFile);
            var outputPath = EnsureOutputPathAskIfMissing(settings.OutputPath);
            DirectoryInfo outputPathInfo = CreateOutputPath(outputPath);
            AzureDevOpsApi templateApi = CreateAzureDevOpsConnection(settings.templateAccessToken, configSettings.template.Organization, configSettings.template.Project);
            var JsonFile = EnsureJsonFileAskIfMissing(settings.inputJsonFile);
            List<jsonWorkItem> jsonWorkItems = LoadJsonFile(settings.inputJsonFile);
            var projectId = EnsureParentIdAskIfMissing(settings.parentId);

            AnsiConsole.WriteLine($"Complete...");




            return 0;
        }


    }
}
