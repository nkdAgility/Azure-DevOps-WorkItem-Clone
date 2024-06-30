using ABB.WorkItemClone.AzureDevOps;
using ABB.WorkItemClone.AzureDevOps.DataContracts;
using ABB.WorkItemClone.ConsoleUI.DataContracts;
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
                        => !string.IsNullOrWhiteSpace(jsonFile)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid JSON file[/]")));
            }
            return jsonFile;
        }

        internal DirectoryInfo CreateOutputPath(string? outputPath)
        {
            outputPath = EnsureOutputPathAskIfMissing(outputPath);
            if (!System.IO.Directory.Exists(outputPath))
            {
                System.IO.Directory.CreateDirectory(outputPath);
            }
            return new DirectoryInfo(outputPath);
        }

        internal string EnsureOutputPathAskIfMissing(string? outputPath)
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

        private string EnsureProjectAskIfMissing(string? project)
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

        private string EnsureOrganizationAskIfMissing(string? organization)
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

    }
}
