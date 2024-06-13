using Spectre.Console.Cli;
using System.ComponentModel;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemExportCommandSettings : BaseCommandSettings
    {
        [CommandArgument(0, "[outputPath]")]
        public string? OutputPath { get; set; }
    }
}