using Spectre.Console.Cli;
using System.ComponentModel;
using Newtonsoft.Json;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemCloneCommandSettings : BaseCommandSettings
    {
        [Description("Execute with no user interaction required.")]
        [CommandOption("--NonInteractive")]
        [JsonIgnore]
        public bool NonInteractive { get; set; }
        [Description("Clear any cache if there is any")]
        [CommandOption("--ClearCache")]
        [JsonIgnore]
        public bool ClearCache { get; set; }
        //------------------------------------------------
        [CommandOption("--outputPath|--cachePath")]
        [DefaultValue("./cache")]
        public string? CachePath { get; set; }
        //------------------------------------------------
        [CommandOption("--jsonFile|--inputJsonFile")]
        public string? inputJsonFile { get; set; }
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
        //------------------------------------------------
    }
}