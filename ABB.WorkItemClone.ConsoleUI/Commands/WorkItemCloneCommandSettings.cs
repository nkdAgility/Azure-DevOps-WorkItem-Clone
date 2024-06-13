using Spectre.Console.Cli;
using System.ComponentModel;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemCloneCommandSettings : BaseCommandSettings
    {
        [CommandArgument(0, "[jsonFile]")]
        public string? JsonFile { get; set; }

        [CommandOption("--targetAccessToken")]
        public string? targetAccessToken { get; set; }

        [CommandOption("-p|--projectId")]
        public int? projectId { get; set; }

    }
}