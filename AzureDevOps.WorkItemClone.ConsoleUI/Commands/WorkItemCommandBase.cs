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
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Microsoft.SqlServer.Server;

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
            config.controlFile = EnsureFileAskIfMissing(config.controlFile = settings.controlFile != null ? settings.controlFile : config.controlFile, "Where is the Control File?");
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

            config.targetQueryTitle = EnsureStringAskIfMissing(config.targetQueryTitle = settings.targetQueryTitle != null ? settings.targetQueryTitle : config.targetQueryTitle, "Provide the target query title?");
            config.targetQueryFolder = EnsureStringAskIfMissing(config.targetQueryFolder = settings.targetQueryFolder != null ? settings.targetQueryFolder : config.targetQueryFolder, "Provide the target query folder?");
            config.targetQuery = EnsureStringAskIfMissing(config.targetQuery = settings.targetQuery != null ? settings.targetQuery : config.targetQuery, "Provide the target WIQL query?");
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

        private JArray DeserializeWorkItemList(string jsonFile)
        {
            JArray configWorkItems;
            try
            {
                configWorkItems = JArray.Parse(File.ReadAllText(jsonFile));
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

        internal JArray DeserializeControlFile(WorkItemCloneCommandSettings config)
        {
           string CachedRunJson =  System.IO.Path.Combine(config.CachePath, config.RunName, "input.json");
            if (System.IO.File.Exists(CachedRunJson))
            {
                // Load From Run Cache
                config.controlFile = CachedRunJson;
                return DeserializeWorkItemList(CachedRunJson);
            } else
            {
                // Load new
                config.controlFile = EnsureFileAskIfMissing(config.controlFile, "Where is the JSON File?");
                if (!System.IO.File.Exists(config.controlFile))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] No JSON file was found.");
                    throw new Exception(config.controlFile + " not found.");
                }

                JArray inputWorkItems;
                inputWorkItems= DeserializeWorkItemList(config.controlFile);
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


        public TypeToLoad FileStoreLoad<TypeToLoad>(string configFile, ConfigFormats format)
        {
            TypeToLoad loadedFromFile;
            configFile = FileStoreEnsureExtension(configFile, format);
            if (!System.IO.File.Exists(configFile))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No file was found.");
                throw new Exception(configFile + " not found.");
            }
            string content = System.IO.File.ReadAllText(configFile);
            switch (format)
            {
                case ConfigFormats.JSON:
                    loadedFromFile = JsonConvert.DeserializeObject<TypeToLoad>(content);
                    break;
                case ConfigFormats.YAML:
                    var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                    loadedFromFile = deserializer.Deserialize<TypeToLoad>(content); /* compile error */
                    break;
                default:
                    throw new Exception("Unknown format");
            }
            return loadedFromFile;
        }

        public bool FileStoreExist(string? configFile, ConfigFormats format)
        {
            return System.IO.File.Exists(FileStoreEnsureExtension(configFile, format));
        }
        public bool FileStoreCheckExtensionMatchesFormat(string? configFile, ConfigFormats format)
        {
            if (Path.GetExtension(configFile).ToLower() != $".{format.ToString().ToLower()}")
            {
                return false;
            }
            return true;
        }

        public string FileStoreEnsureExtension(string? configFile, ConfigFormats format)
        {
            if (Path.GetExtension(configFile).ToLower() != $".{format.ToString().ToLower()}")
            {
                var original = configFile;
                configFile = Path.ChangeExtension(configFile, format.ToString().ToLower());
                AnsiConsole.MarkupLine($"[green]Info:[/] Changed name of {original} to {configFile} ");
            }
            return configFile;
        }

        public string FileStoreSave<TypeToSave>(string configFile, TypeToSave content , ConfigFormats format)
        {
            string output;
            switch (format)
            {
                case ConfigFormats.JSON:
                    output = JsonConvert.SerializeObject(content, Formatting.Indented);
                    break;
                case ConfigFormats.YAML:
                    var serializer = new SerializerBuilder().Build();
                    output =  serializer.Serialize(content);
                    break;
                default:
                    throw new Exception("Unknown format");
            }
            configFile = FileStoreEnsureExtension(configFile, format);
            System.IO.File.WriteAllText(configFile, output);
            return output;
        }

        internal string EnsureFileAskIfMissing(string? filename, string message = "What file should we load?")
        {
            if (filename == null)
            {

                filename = AnsiConsole.Prompt(
                new TextPrompt<string>(message)
                    .Validate(configFile
                        => !string.IsNullOrWhiteSpace(configFile)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[yellow]Invalid config file[/]")));
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
               .AddRow("controlFile", config.controlFile != null ? config.controlFile : "NOT SET")
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
