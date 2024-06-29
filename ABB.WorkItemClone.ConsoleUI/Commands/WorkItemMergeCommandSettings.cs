using Spectre.Console.Cli;
using System.ComponentModel;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemMergeCommandSettings : WorkItemExportCommandSettings
    {
        [CommandOption("--NonInteractive")]
        public bool NonInteractive { get; set; }
        [CommandOption("--ClearCache")]
        public bool ClearCache { get; set; }

        [CommandOption("--outputPath")]
        public string? OutputPath { get; set; }

        [CommandOption("--jsonFile")]
        public string? JsonFile { get; set; }

        [CommandOption("--targetAccessToken")]
        public string? targetAccessToken { get; set; }

        [CommandOption("-p|--projectId")]
        public int? projectId { get; set; }

    }
}