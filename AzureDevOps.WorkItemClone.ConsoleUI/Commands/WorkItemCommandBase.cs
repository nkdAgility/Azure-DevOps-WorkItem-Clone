using ABB.WorkItemClone.AzureDevOps;
using ABB.WorkItemClone.AzureDevOps.DataContracts;
using ABB.WorkItemClone.ConsoleUI.DataContracts;
using Microsoft.VisualStudio.Services.Identity;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal abstract class WorkItemCommandBase<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
    {

        internal int EnsureParentIdAskIfMissing(int? parentId)
        {
            if (parentId == null)
            {
                parentId = AnsiConsole.Prompt(
                new TextPrompt<int>("What is the parent Id?")
                    .Validate(projectId
                        => projectId > 0
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid parent Id[/]")));
            }
            return parentId.Value;
        }

        internal List<jsonWorkItem> LoadJsonFile(string? jsonFile)
        {
            jsonFile = EnsureJsonFileAskIfMissing(jsonFile);
            if (!System.IO.File.Exists(jsonFile))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No JSON file was found.");
                throw new Exception(jsonFile + " not found.");
            }
            List<jsonWorkItem> configWorkItems;
            try
            {
                configWorkItems = JsonConvert.DeserializeObject<List<jsonWorkItem>>(File.ReadAllText(jsonFile));
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

        internal string EnsureJsonFileAskIfMissing(string? jsonFile)
        {
            if (jsonFile == null)
            {
                jsonFile = AnsiConsole.Prompt(
                new TextPrompt<string>("Where is the JSON File?")
                    .Validate(jsonFile
                        => !string.IsNullOrWhiteSpace(jsonFile) && System.IO.File.Exists(jsonFile)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid JSON file[/]")));
            }
            return jsonFile;
        }

        internal DirectoryInfo CreateOutputPath(string? outputPath)
        {
            outputPath = EnsureCachePathAskIfMissing(outputPath);
            if (!System.IO.Directory.Exists(outputPath))
            {
                System.IO.Directory.CreateDirectory(outputPath);
            }
            return new DirectoryInfo(outputPath);
        }

        internal string EnsureCachePathAskIfMissing(string? outputPath)
        {
            if (outputPath == null)
            {
                outputPath = AnsiConsole.Prompt(
                new TextPrompt<string>("What is the output path?")
                    .Validate(outputPath
                        => !string.IsNullOrWhiteSpace(outputPath)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid output path[/]")));
            }
            return outputPath;
        }

        internal AzureDevOpsApi CreateAzureDevOpsConnection(string? accessToken, string? organization, string? project)
        {
            organization = EnsureOrganizationAskIfMissing(organization);
            project = EnsureProjectAskIfMissing(project);
            accessToken = EnsureAccessTokenAskIfMissing(accessToken, organization);
            return new AzureDevOpsApi(accessToken, organization, project);
        }

        internal string EnsureProjectAskIfMissing(string? project)
        {
            if (project == null)
            {

                project = AnsiConsole.Prompt(
                new TextPrompt<string>("What is the project?")
                    .Validate(project
                        => !string.IsNullOrWhiteSpace(project)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid project[/]")));
            }
            return project;
        }

        internal string EnsureOrganizationAskIfMissing(string? organization)
        {
            if (organization == null)
            {

                organization = AnsiConsole.Prompt(
                new TextPrompt<string>("What is the organization?")
                    .Validate(organization
                        => !string.IsNullOrWhiteSpace(organization)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid organization[/]")));
            }
            return organization;
        }

        private string EnsureAccessTokenAskIfMissing(string? accessToken, string organization)
        {
            if (accessToken == null)
            {

                accessToken = AnsiConsole.Prompt(
                new TextPrompt<string>($"Provide a valid Access Token for {organization}?")
                    .Validate(accessToken
                        => !string.IsNullOrWhiteSpace(accessToken)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid access token[/]")));
            }
            return accessToken;
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
               .AddRow("configFile", config.configFile != null ? config.configFile : "NOT SET")
               .AddRow("CachePath",  config.CachePath != null ? config.CachePath : "NOT SET")
               .AddRow("templateAccessToken", "***************")
               .AddRow("templateOrganization", config.templateOrganization != null ? config.templateOrganization : "NOT SET")
               .AddRow("templateProject", config.templateProject != null ? config.templateProject : "NOT SET")
               .AddRow("targetAccessToken", "***************")
               .AddRow("targetOrganization", config.targetOrganization != null ? config.targetOrganization : "NOT SET")
               .AddRow("targetProject", config.targetProject != null ? config.targetProject : "NOT SET")
               .AddRow("targetParentId", config.targetParentId != null ? config.targetParentId.ToString() : "NOT SET")
               .AddRow("inputJsonFile", config.inputJsonFile != null ? config.inputJsonFile : "NOT SET")
                );
        }

    }
}
