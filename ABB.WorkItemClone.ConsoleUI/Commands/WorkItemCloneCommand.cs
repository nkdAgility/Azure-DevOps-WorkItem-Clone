using ABB.WorkItemClone.AzureDevOps;
using ABB.WorkItemClone.AzureDevOps.DataContracts;
using ABB.WorkItemClone.ConsoleUI.DataContracts;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemCloneCommand : Command<WorkItemCloneCommandSettings>
    {
        public override int Execute(CommandContext context, WorkItemCloneCommandSettings settings)
        {
            AnsiConsole.Write(new Rule("Clone Work Items").LeftJustified());


            List<MergeWorkItem> mergeWorkItems;
            try
            {
                mergeWorkItems = JsonConvert.DeserializeObject<List<MergeWorkItem>>(File.ReadAllText(settings.JsonFile));
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No JSON file was malformed.");
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                return 2;
            }
            if (mergeWorkItems?.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No JSON file is empty.");
                return 3;
            }

            AnsiConsole.MarkupLine($"[green]Merge Items Loaded:[/] {mergeWorkItems?.Count}.");


            // Get Template

            if (settings.AccessToken == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No Access Token was provided.");
                return 4;
            }
            if (settings.Project == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No project was provided.");
                return 4;
            }
            if (settings.Account == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No account was provided.");
                return 4;
            }
            AzureDevOpsApi api = new AzureDevOpsApi(settings.AccessToken, settings.Account, settings.Project);
            var workItems = api.GetWiqlQueryResults().Result;
            AnsiConsole.MarkupLine($"[green]Work Items Found:[/] {workItems?.workItems.Count()}.");

         
            foreach (var item in mergeWorkItems)
            {
                AnsiConsole.MarkupLine($"[green]Building:[/] {item.id}.");
                var workItem = api.GetWorkItem((int)item.id).Result;
                if (workItem != null)
                {
                    workItem.fields.SystemId = 0;
                    workItem.fields.SystemTitle = item.fields.title;
                    //workItemsForClone.Add(workItem);

                }
            }
          



            return 0;
        }
    }
}
