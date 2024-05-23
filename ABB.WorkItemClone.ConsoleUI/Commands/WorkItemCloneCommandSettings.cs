using Spectre.Console.Cli;
using System.ComponentModel;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemCloneCommandSettings : CommandSettings
    {
        [CommandArgument(0, "[jsonFile]")]
        public string? JsonFile { get; set; }

        [CommandOption("-t|--accessToken")]
        public string? AccessToken { get; set; }

        [CommandOption("-a|--account")]
        [DefaultValue("ABB-MO-ATE")]
        public string? Account { get; set; }

        [CommandOption("-p|--project")]
        [DefaultValue("ABB Traction Template")]
        public string? Project { get; set; }

    }
}