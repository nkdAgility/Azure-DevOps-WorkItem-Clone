using AzureDevOps.WorkItemClone;
using AzureDevOps.WorkItemClone.DataContracts;
using AzureDevOps.WorkItemClone.ConsoleUI.DataContracts;
using Microsoft.VisualStudio.Services.Identity;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDevOps.WorkItemClone.ConsoleUI.Commands
{
    internal abstract class WorkItemCommandBase<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
    {

        internal void CombineValuesFromConfigAndSettings(WorkItemCloneCommandSettings settings, WorkItemCloneCommandSettings config)
        {
            config.NonInteractive = settings.NonInteractive;
            config.ClearCache = settings.ClearCache;
            config.RunName = settings.RunName != null ? settings.RunName : DateTime.Now.ToString("yyyyyMMddHHmmss");
            config.configFile = EnsureFileAskIfMissing(config.configFile = settings.configFile != null ? settings.configFile : config.configFile, "Where is the config file to load?");
            config.inputJsonFile = EnsureFileAskIfMissing(config.inputJsonFile = settings.inputJsonFile != null ? settings.inputJsonFile : config.inputJsonFile, "Where is the JSON File?");
            config.CachePath = EnsureFolderAskIfMissing(config.CachePath = settings.CachePath != null ? settings.CachePath : config.CachePath, "What is the cache path?");

            config.templateOrganization = EnsureStringAskIfMissing(config.templateOrganization = settings.templateOrganization != null ? settings.templateOrganization : config.templateOrganization, "What is the template organisation?");
            config.templateProject = EnsureStringAskIfMissing(config.templateProject = settings.templateProject != null ? settings.templateProject : config.templateProject, $"What is the project on {config.templateOrganization}?");
            config.templateAccessToken = EnsureStringAskIfMissing(settings.templateAccessToken != null ? settings.templateAccessToken : config.templateAccessToken, $"What is the access token on {config.templateOrganization}?");
            config.templateParentId = EnsureIntAskIfMissing(config.templateParentId = settings.templateParentId != null ? settings.templateParentId : config.templateParentId, "Provide the template parent?");

            config.targetOrganization = EnsureStringAskIfMissing(config.targetOrganization = settings.targetOrganization != null ? settings.targetOrganization : config.targetOrganization, "What is the target organisation?");
            config.targetProject = EnsureStringAskIfMissing(config.targetProject = settings.targetProject != null ? settings.targetProject : config.targetProject, $"What is the project on {config.targetOrganization}?");
            config.targetAccessToken = EnsureStringAskIfMissing(settings.targetAccessToken != null ? settings.targetAccessToken : config.targetAccessToken, $"What is the access token on {config.targetOrganization}?");
            config.targetParentId = EnsureIntAskIfMissing(config.targetParentId = settings.targetParentId != null ? settings.targetParentId : config.targetParentId, "Provide the target parent?");
            config.targetFalbackWit = EnsureStringAskIfMissing(config.targetFalbackWit = settings.targetFalbackWit != null ? settings.targetFalbackWit : config.targetFalbackWit, "Provide the target fallback wit?");

        }



        internal int EnsureIntAskIfMissing(int? value, string message = "Provide a valid number?")
        {
            if (value == null)
            {
                value = AnsiConsole.Prompt(
                new TextPrompt<int>(message)
                    .Validate(projectId
                        => projectId > 0
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid number[/]")));
            }
            return value.Value;
        }

        private List<jsonWorkItem1> DeserializeWorkItemList(string jsonFile)
        {
            List<jsonWorkItem1> configWorkItems;
            try
            {
                configWorkItems = JsonConvert.DeserializeObject<List<jsonWorkItem1>>(File.ReadAllText(jsonFile));
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No JSON file was malformed.");
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                throw new Exception(jsonFile + " is malformed.");
            }
            if (configWorkItems?.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No JSON file is empty.");
                throw new Exception(jsonFile + " is empty.");
            }
            return configWorkItems;
        }

        internal List<jsonWorkItem1> DeserializeWorkItemList(WorkItemCloneCommandSettings config)
        {
           string CachedRunJson =  System.IO.Path.Combine(config.CachePath, config.RunName, "input.json");
            if (System.IO.File.Exists(CachedRunJson))
            {
                // Load From Run Cache
                config.inputJsonFile = CachedRunJson;
                return DeserializeWorkItemList(CachedRunJson);
            } else
            {
                // Load new
                config.inputJsonFile = EnsureFileAskIfMissing(config.inputJsonFile, "Where is the JSON File?");
                if (!System.IO.File.Exists(config.inputJsonFile))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] No JSON file was found.");
                    throw new Exception(config.inputJsonFile + " not found.");
                }

                List<jsonWorkItem1> inputWorkItems;
                inputWorkItems= DeserializeWorkItemList(config.inputJsonFile);
                System.IO.File.WriteAllText(CachedRunJson, JsonConvert.SerializeObject(inputWorkItems, Formatting.Indented));
                return inputWorkItems;
            } 
        }


        internal DirectoryInfo CreateOutputPath(string? outputPath)
        {
            outputPath = EnsureFolderAskIfMissing(outputPath, "What cache path should we use?");
            if (!System.IO.Directory.Exists(outputPath))
            {
                System.IO.Directory.CreateDirectory(outputPath);
            }
            return new DirectoryInfo(outputPath);
        }


        internal AzureDevOpsApi CreateAzureDevOpsConnection(string? accessToken, string? organization, string? project)
        {
            return new AzureDevOpsApi(accessToken, organization, project);
        }

        internal string EnsureStringAskIfMissing(string value, string message)
        {
            if (value == null)
            {

                value = AnsiConsole.Prompt(
                new TextPrompt<string>(message)
                    .Validate(value
                        => !string.IsNullOrWhiteSpace(value)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid value[/]")));
            }
            return value;
        }


        internal ConfigurationSettings LoadConfigFile(string? configFile)
        {
            ConfigurationSettings configSettings = System.Text.Json.JsonSerializer.Deserialize<ConfigurationSettings>(System.IO.File.ReadAllText(configFile));
            return configSettings;
        }

  internal WorkItemCloneCommandSettings LoadWorkItemCloneCommandSettingsFromFile(string? configFile)
        {
            WorkItemCloneCommandSettings configSettings = System.Text.Json.JsonSerializer.Deserialize<WorkItemCloneCommandSettings>(System.IO.File.ReadAllText(configFile));
            return configSettings;
        }

        internal string EnsureFileAskIfMissing(string? filename, string message = "What file should we load?")
        {
            if (filename == null)
            {

                filename = AnsiConsole.Prompt(
                new TextPrompt<string>("Where is the config File?")
                    .Validate(configFile
                        => !string.IsNullOrWhiteSpace(configFile) && System.IO.File.Exists(configFile)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid config file[/]")));
            }
            if (!System.IO.File.Exists(filename))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No file was found.");
                throw new Exception(filename + " not found.");
            }
            return filename;
        }

        internal string EnsureFolderAskIfMissing(string? foldername, string message = "What folder should we use?")
        {
            if (foldername == null)
            {

                foldername = AnsiConsole.Prompt(
                new TextPrompt<string>("Where is the folder?")
                    .Validate(configFile
                        => !string.IsNullOrWhiteSpace(configFile) && System.IO.Directory.Exists(configFile)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid config file[/]")));
            }
            return foldername;
        }


        internal string EnsureConfigFileAskIfMissing(string? configFile)
        {
            if (configFile == null)
            {

                configFile = AnsiConsole.Prompt(
                new TextPrompt<string>("Where is the config File?")
                    .Validate(configFile
                        => !string.IsNullOrWhiteSpace(configFile)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid config file[/]")));
            }
            if (!System.IO.File.Exists(configFile))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No JSON file was found.");
                throw new Exception(configFile + " not found.");
            }
            return configFile;
        }

        internal void WriteOutSettings(WorkItemCloneCommandSettings config)
        {
            AnsiConsole.Write(
           new Table()
               .AddColumn(new TableColumn("Setting").Alignment(Justify.Right))
               .AddColumn(new TableColumn("Value"))
               .AddEmptyRow()
               .AddRow("runName", config.RunName != null ? config.RunName : "NOT SET")
               .AddEmptyRow()
               .AddRow("configFile", config.configFile != null ? config.configFile : "NOT SET")               
               .AddRow("CachePath",  config.CachePath != null ? config.CachePath : "NOT SET")
               .AddRow("inputJsonFile", config.inputJsonFile != null ? config.inputJsonFile : "NOT SET")
               .AddEmptyRow()
               .AddRow( "templateAccessToken", "***************")
               .AddRow("templateOrganization", config.templateOrganization != null ? config.templateOrganization : "NOT SET")
               .AddRow("templateProject", config.templateProject != null ? config.templateProject : "NOT SET")
               .AddRow("templateParentId", config.templateParentId != null ? config.templateParentId.ToString() : "NOT SET")
               .AddEmptyRow()
               .AddRow("targetAccessToken", "***************")
               .AddRow("targetOrganization", config.targetOrganization != null ? config.targetOrganization : "NOT SET")
               .AddRow("targetProject", config.targetProject != null ? config.targetProject : "NOT SET")
               .AddRow("targetParentId", config.targetParentId != null ? config.targetParentId.ToString() : "NOT SET")
               .AddRow("targetFalbackWit", config.targetFalbackWit != null ? config.targetFalbackWit : "NOT SET")
               
                );
        }

    }
}
