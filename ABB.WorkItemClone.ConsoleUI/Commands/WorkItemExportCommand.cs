using ABB.WorkItemClone.AzureDevOps;
using ABB.WorkItemClone.AzureDevOps.DataContracts;
using ABB.WorkItemClone.ConsoleUI.DataContracts;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemExportCommand : Command<WorkItemExportCommandSettings>
    {
        public override int Execute(CommandContext context, WorkItemExportCommandSettings settings)
        {
            AnsiConsole.Write(new Rule("Export Work Items").LeftJustified());
            // Load Config
            if (settings.configFile == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No JSON file was provided.");
                return 1;
            }
            if (!System.IO.File.Exists(settings.configFile))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No JSON file was found.");
                return 1;
            }
            ConfigurationSettings configSettings = JsonConvert.DeserializeObject<ConfigurationSettings>(System.IO.File.ReadAllText(settings.configFile));



            if (settings.templateAccessToken == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No Access Token was provided on command line.");
                return 4;
            }
            if (configSettings.template.Project == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No project was provided in configuration.");
                return 4;
            }
            if (configSettings.template.Organization == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No account was provided in configuration.");
                return 4;
            }
            if (settings.OutputPath == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No output path was provided on command line.");
                return 4;
            }
            if (!System.IO.Directory.Exists(settings.OutputPath))
            {
                System.IO.Directory.CreateDirectory(settings.OutputPath);
            }


            AzureDevOpsApi templateApi = new AzureDevOpsApi(settings.templateAccessToken, configSettings.template.Organization, configSettings.template.Project);
            var workItems = templateApi.GetWiqlQueryResults().Result;
            AnsiConsole.MarkupLine($"[green]Work Items Found:[/] {workItems?.workItems.Count()}.");

            AnsiConsole.MarkupLine($"[green]Output Path:[/] {Path.GetFullPath(settings.OutputPath)}.");

            foreach (var item in workItems.workItems)
            {
                var wiFilePath = System.IO.Path.Combine(settings.OutputPath, $"{item.id}.json");
                var wiFileRelativePath = System.IO.Path.GetRelativePath(settings.OutputPath, wiFilePath);
                var workItem = templateApi.GetWorkItem((int)item.id).Result;
                if (workItem != null)
                {
                    
                    System.IO.File.WriteAllText(wiFilePath, JsonConvert.SerializeObject(workItem, Formatting.Indented));
                   AnsiConsole.MarkupLine($"[green]Exported to:[/] {wiFileRelativePath}.");
                }
            }

            AnsiConsole.MarkupLine($"[green]Exported to:[/] {System.IO.Path.GetFullPath(settings.OutputPath)}.");

            return 0;
        }
    }
}
