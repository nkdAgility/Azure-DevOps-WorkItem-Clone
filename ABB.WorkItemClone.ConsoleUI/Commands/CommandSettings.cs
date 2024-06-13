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
        [CommandOption("--templateAccessToken")]
        public string? templateAccessToken { get; set; }

        [CommandOption("--config")]
        [DefaultValue("configuration.json")]
        public string? configFile { get; set; }
    }
}
