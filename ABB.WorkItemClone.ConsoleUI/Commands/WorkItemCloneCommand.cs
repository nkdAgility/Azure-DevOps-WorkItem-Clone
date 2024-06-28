using ABB.WorkItemClone.AzureDevOps;
using ABB.WorkItemClone.AzureDevOps.DataContracts;
using ABB.WorkItemClone.ConsoleUI.DataContracts;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemCloneCommand : Command<WorkItemCloneCommandSettings>
    {
        public override int Execute(CommandContext context, WorkItemCloneCommandSettings settings)
        {
            AnsiConsole.Write(new Rule("Clone Work Items").LeftJustified());

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


            List<MergeWorkItem> configWorkItems;
            try
            {
                configWorkItems = JsonConvert.DeserializeObject<List<MergeWorkItem>>(File.ReadAllText(settings.JsonFile));
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No JSON file was malformed.");
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                return 2;
            }
            if (configWorkItems?.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No JSON file is empty.");
                return 3;
            }

            AnsiConsole.MarkupLine($"[green]Merge Items Loaded:[/] {configWorkItems?.Count}.");


            AzureDevOpsApi templateApi = new AzureDevOpsApi(settings.templateAccessToken, configSettings.template.Organization, configSettings.template.Project);
            var workItems = templateApi.GetWiqlQueryResults().Result;
            AnsiConsole.MarkupLine($"[green]Work Items Found:[/] {workItems?.workItems.Count()}.");

            AzureDevOpsApi targetApi = new AzureDevOpsApi(settings.targetAccessToken, configSettings.target.Organization, configSettings.target.Project);
            WorkItemFull projectItem =  targetApi.GetWorkItem((int)settings.projectId).Result;

            List< WorkItemAdd > test = new List<WorkItemAdd>();

            foreach (var cWorkItem in configWorkItems)
            {
                AnsiConsole.MarkupLine($"[green]Building:[/] {cWorkItem.id}.");
                WorkItemFull templateItem = null;
                if (cWorkItem.id != null)
                {
                    templateItem = templateApi.GetWorkItem((int)cWorkItem.id).Result;
                }

                WorkItemAdd itemAdd = new WorkItemAdd();
                itemAdd.ItemFromtemplate = templateItem;
                itemAdd.ItemFromConfig = cWorkItem;

               
                itemAdd.Operations.Add(new FieldOperation() { op = "add", path = "/fields/System.Title", value = $"{cWorkItem.fields.title}" });
                itemAdd.Operations.Add(new FieldOperation() { op = "add", path = "/fields/Custom.Product", value = $"{cWorkItem.fields.product}" });

                if (templateItem != null)
                {
                    itemAdd.Operations.Add(new FieldOperation() { op = "add", path = "/fields/System.Description", value = templateItem.fields.SystemDescription });
                    itemAdd.Operations.Add(new FieldOperation() { op = "add", path = "/fields/Microsoft.VSTS.Common.AcceptanceCriteria", value = templateItem.fields.MicrosoftVSTSCommonAcceptanceCriteria });
                }
                itemAdd.Operations.Add(new FieldOperation() { op = "add", path = "/fields/System.AreaPath", value = string.Join("\\", configSettings.target.Project, cWorkItem.area) });
                itemAdd.Operations.Add(new FieldOperation() { op = "add", path = "/fields/System.Tags", value = string.Join(";" , cWorkItem.tags, cWorkItem.area, cWorkItem.fields.product, templateItem != null? templateItem.fields.SystemTags : "") });
                itemAdd.Operations.Add(new RelationOperation() { op = "add", path = "/relations/-", value = new RelationValue { rel = "System.LinkTypes.Hierarchy-Reverse", url = projectItem.url } });           


                test.Add(itemAdd);
              

            }

            AnsiConsole.MarkupLine($"[green]Built :[/] {test?.Count()}.");

            /// Actual Add
            /// 
            foreach (var itemAdd in test)
            {

                WorkItemFull newWorkItem = targetApi.CreateWorkItem(itemAdd, "Dependancy").Result;
                AnsiConsole.MarkupLine($"[green]Added:[/] {newWorkItem.id}.");
            }



            return 0;
        }



    }
}
