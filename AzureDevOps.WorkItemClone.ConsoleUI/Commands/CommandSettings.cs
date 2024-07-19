using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace AzureDevOps.WorkItemClone.ConsoleUI.Commands
{
    internal class BaseCommandSettings : CommandSettings
    {
        [Description("Pre configure paramiters using this config file. Run `Init` to create it.")]
        [CommandOption("--config|--configFile")]
        [DefaultValue("configuration.json")]
        [JsonIgnore, YamlIgnore]
        public string? configFile { get; set; }
    }
}
