using Spectre.Console.Cli;
using System.ComponentModel;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace AzureDevOps.WorkItemClone.ConsoleUI.Commands
{
    public enum ConfigFormats
    {
       JSON,
       YAML
    }

    internal class WorkItemCloneCommandSettings : BaseCommandSettings
    {
        //------------------------------------------------
        [Description("Execute with no user interaction required.")]
        [CommandOption("--NonInteractive")]
        [JsonIgnore, YamlIgnore]
        public bool NonInteractive { get; set; }
        [Description("Clear any cache if there is any")]
        [CommandOption("--ClearCache")]
        [JsonIgnore, YamlIgnore]
        public bool ClearCache { get; set; }
        [Description("Use this run name to execute. This will create a unique folder under the CachePath for storing run specific data and status. Defaults to yyyyyMMddHHmmss.")]
        [CommandOption("--RunName")]
        [JsonIgnore, YamlIgnore]
        public string? RunName { get; set; }
        [Description("Use this run name to execute. This will create a unique folder under the CachePath for storing run specific data and status. Defaults to yyyyyMMddHHmmss.")]
        [CommandOption("--configFormat")]
        [DefaultValue(ConfigFormats.JSON)]
        [JsonIgnore, YamlIgnore]
        public ConfigFormats ConfigFormat { get; set; }
        //------------------------------------------------
        [CommandOption("--outputPath|--cachePath")]
        [DefaultValue("./cache")]
        public string? CachePath { get; set; }
        //------------------------------------------------
        [CommandOption("--jsonFile|--inputJsonFile|--controlFile")]
        public string? controlFile { get; set; }
        //------------------------------------------------
        [Description("The access token for the target location")]
        [CommandOption("--targetAccessToken")]
        public string? targetAccessToken { get; set; }
        [Description("The Organization name for the target location")]
        [CommandOption("--targetOrganization")]
        public string? targetOrganization { get; set; }
        [Description("The project name for the target location")]
        [CommandOption("--targetProject")]
        public string? targetProject { get; set; }
        [Description("The ID of the work item in the target environment that will be the parent of all created work items.")]
        [CommandOption("-p|--parentId|--targetParentId")]
        public int? targetParentId { get; set; }
        [Description("This is the fallback work item type that will be used if we cant find one!")]
        [CommandOption("--targetFalbackWit")]
        [DefaultValue("Deliverable")]
        public string? targetFalbackWit { get; set; }


        [Description("The WIQL Query to use. You can use @projectID, @projectTitle, @projectTags to replace data from the project!")]
        [CommandOption("--targetQuery")]
        public string? targetQuery { get; set; }

        [Description("The title to use for the query. You can use @projectID, @projectTitle, @projectTags, @RunName to replace data from the project!")]
        [CommandOption("--targetQueryTitle")]
        public string? targetQueryTitle { get; set; }

        [Description("Must already Exist and be in the form 'Shared Queries/Folder1/Folder2'!")]
        [CommandOption("--targetQueryFolder")]
        public string? targetQueryFolder { get; set; }

        //------------------------------------------------
        [Description("The access token for the template location")]
        [CommandOption("--templateAccessToken")]
        public string? templateAccessToken { get; set; }
        [Description("The Organization name for the template location")]
        [CommandOption("--templateOrganization")]
        public string? templateOrganization { get; set; }
        [Description("The project name for the template location")]
        [CommandOption("--templateProject")]
        public string? templateProject { get; set; }
        [Description("The ID of the work item in the template environment under which we will read all the sub items.")]
        [CommandOption("--templateParentId")]
        public int? templateParentId { get; set; }
        //------------------------------------------------
    }
}