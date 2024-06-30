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
            config.inputJsonFile = EnsureJsonFileAskIfMissing(config.inputJsonFile = settings.inputJsonFile != null ? settings.inputJsonFile : config.inputJsonFile);
            config.CachePath = EnsureCachePathAskIfMissing(config.CachePath = settings.CachePath != null ? settings.CachePath : config.CachePath);
            
            config.templateOrganization = EnsureOrganizationAskIfMissing(config.templateOrganization = settings.templateOrganization != null ? settings.templateOrganization : config.templateOrganization);
            config.templateProject = EnsureProjectAskIfMissing(config.templateProject = settings.templateProject != null ? settings.templateProject : config.templateProject);

            config.targetOrganization = EnsureOrganizationAskIfMissing(config.targetOrganization = settings.targetOrganization != null ? settings.targetOrganization : config.targetOrganization);
            config.targetProject = EnsureProjectAskIfMissing(config.targetProject = settings.targetProject != null ? settings.targetProject : config.targetProject);
            config.targetParentId = EnsureParentIdAskIfMissing(config.targetParentId = settings.targetParentId != null ? settings.targetParentId : config.targetParentId);


            System.IO.File.WriteAllText(configFile, JsonConvert.SerializeObject(config, Formatting.Indented));

            return 0;

            //

            //WorkItemCloneCommandSettings configSettings = null;
            //if (File.Exists(configFile))
            //{
            //    var proceedWithSettings = AnsiConsole.Prompt(
            //    new SelectionPrompt<bool> { Converter = value => value ? "Yes" : "No" }
            //        .Title("The config file name used exists would you like to load this one?")
            //        .AddChoices(true, false));
            //    if (proceedWithSettings)
            //    {
            //        configSettings = LoadWorkItemCloneCommandSettingsFromFile(configFile);
            //    } else
            //    {
            //        configSettings = new WorkItemCloneCommandSettings();
            //    }
            //}

            //configSettings.CachePath = EnsureOutputPathAskIfMissing(settings.CachePath != null ?  );
            //configSettings.


            //DirectoryInfo outputPathInfo = CreateOutputPath(outputPath);
            //AzureDevOpsApi templateApi = CreateAzureDevOpsConnection(settings.templateAccessToken, configSettings.template.Organization, configSettings.template.Project);
            //var JsonFile = EnsureJsonFileAskIfMissing(settings.inputJsonFile);
            //List<jsonWorkItem> jsonWorkItems = LoadJsonFile(settings.inputJsonFile);
            //var projectId = EnsureParentIdAskIfMissing(settings.parentId);

            AnsiConsole.WriteLine($"Complete...");




            return 0;
        }


    }
}
