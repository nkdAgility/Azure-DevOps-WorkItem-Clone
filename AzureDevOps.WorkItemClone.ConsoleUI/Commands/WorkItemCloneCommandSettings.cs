using Spectre.Console.Cli;
using System.ComponentModel;
using Newtonsoft.Json;

namespace AzureDevOps.WorkItemClone.ConsoleUI.Commands
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
        [Description("Use this run name to execute. This will create a unique folder under the CachePath for storing run specific data and status. Defaults to yyyyyMMddHHmmss.")]
        [CommandOption("--RunName")]
        [JsonIgnore]
        public string? RunName { get; set; }
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
        [DefaultValue("SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AreaPath],[System.AssignedTo],[System.State] FROM workitems WHERE [System.Parent] = @projectID")]
        public string? targetQuery { get; set; }

        [Description("The title to use for the query. You can use @projectID, @projectTitle, @projectTags, @RunName to replace data from the project!")]
        [CommandOption("--targetQueryTitle")]
        [DefaultValue("Project-@RunName - @projectTitle")]
        public string? targetQueryTitle { get; set; }

        [Description("Must already Exist and be in the form 'Shared Queries/Folder1/Folder2'!")]
        [CommandOption("--targetQueryFolder")]
        [DefaultValue("Shared Queries")]
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