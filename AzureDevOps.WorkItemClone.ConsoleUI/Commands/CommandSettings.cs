using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class BaseCommandSettings : CommandSettings
    {
        [Description("Pre configure paramiters using this config file. Run `Init` to create it.")]
        [CommandOption("--config")]
        [DefaultValue("configuration.json")]
        public string? configFile { get; set; }
    }
}
