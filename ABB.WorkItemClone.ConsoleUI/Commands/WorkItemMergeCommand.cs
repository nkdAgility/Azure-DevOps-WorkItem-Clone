using ABB.WorkItemClone.AzureDevOps;
using ABB.WorkItemClone.AzureDevOps.DataContracts;
using ABB.WorkItemClone.ConsoleUI.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models.Process;
using Microsoft.VisualStudio.Services.TestManagement.TestPlanning.WebApi;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System;

namespace ABB.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemMergeCommand : WorkItemCommandBase<WorkItemMergeCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, WorkItemMergeCommandSettings settings)
        {


            var configFile = EnsureConfigFileAskIfMissing(settings.configFile);
            ConfigurationSettings configSettings = LoadConfigFile(settings.configFile);
            var outputPath = EnsureOutputPathAskIfMissing(settings.OutputPath);
            DirectoryInfo outputPathInfo = CreateOutputPath(outputPath);
            AzureDevOpsApi templateApi = CreateAzureDevOpsConnection(settings.templateAccessToken, configSettings.template.Organization, configSettings.template.Project);
            var JsonFile = EnsureJsonFileAskIfMissing(settings.JsonFile);
            List<jsonWorkItem> jsonWorkItems = LoadJsonFile(settings.JsonFile);
            var projectId = EnsureProjectIdAskIfMissing(settings.projectId);


            AnsiConsole.Write(
            new Table()
                .AddColumn(new TableColumn("Setting").Alignment(Justify.Right))
                .AddColumn(new TableColumn("Value"))
                .AddRow("configFile", configFile)
                .AddRow("outputPath", outputPath)
                .AddRow("templateAccessToken", "***************")
                .AddRow("templateOrganization", configSettings.template.Organization)
                .AddRow("templateProject", configSettings.template.Project)
                .AddRow("targetAccessToken", "***************")
                .AddRow("targetOrganization", configSettings.target.Organization)
                .AddRow("targetProject", configSettings.target.Project)
                .AddRow("projectId", projectId.ToString())
                .AddRow("JsonFile", JsonFile)
                 );
            if (!settings.NonInteractive)
            {

                var proceedWithSettings = AnsiConsole.Prompt(
                new SelectionPrompt<bool> { Converter = value => value ? "Yes" : "No" }
                    .Title("Proceed with the aforementioned settings?")
                    .AddChoices(true, false)
            );
            }

            await AnsiConsole.Progress()
             .AutoClear(false)   // Do not remove the task list when done
             .HideCompleted(false)   // Hide tasks as they are completed
             .Columns(new ProgressColumn[]
             {
                            new TaskDescriptionColumn() { Alignment = Justify.Left },    // Task description
                            new ProgressBarColumn(),        // Progress bar
                            new PercentageColumn(),         // Percentage
                            new RemainingTimeColumn(),      // Remaining time
                            new SpinnerColumn(),            // Spinner
             })
             .StartAsync(async ctx =>
             {
                 // Define tasks
                 var task1 = ctx.AddTask("[bold]Stage 1[/]: Get Template Items", false);
                 var task2 = ctx.AddTask("[bold]Stage 2[/]: Load Template Items", false);
                 var task3 = ctx.AddTask("[bold]Stage 3[/]: Get Target Project", false);
                 var task4 = ctx.AddTask("[bold]Stage 4[/]: Create Work Items (First Pass)", false);
                 var task5 = ctx.AddTask("[bold]Stage 5[/]: Create Work Item (Second Pass) Relations ", false);


                 // Task 1: query for template work items
                 task1.StartTask();
                 task1.MaxValue = 1;
                 AnsiConsole.WriteLine("Stage 1: Executing items from Query");
                 var fakeItemsFromTemplateQuery = await templateApi.GetWiqlQueryResults();
                 AnsiConsole.WriteLine($"Stage 1: Query returned {fakeItemsFromTemplateQuery.workItems.Count()} items id's from the template.");
                 task1.Increment(1);
                 task1.StopTask();

                

                 // Task 2: getting work items and their full data
                 task2.MaxValue = fakeItemsFromTemplateQuery.workItems.Count();
                 task2.StartTask();
                 AnsiConsole.WriteLine($"Stage 2: Starting process of {task2.MaxValue} work items to get their full data ");
                 List<WorkItemFull> templateWorkItems = new List<WorkItemFull>();
                 await foreach (var workItem in templateApi.GetWorkItemsFullAsync(fakeItemsFromTemplateQuery.workItems))
                 {
                     //AnsiConsole.WriteLine($"Stage 2: Processing  {workItem.id}:`{workItem.fields.SystemTitle}`");
                     templateWorkItems.Add(workItem);
                     task2.Increment(1);
                 }
                 AnsiConsole.WriteLine($"Stage 2: All {task2.MaxValue} work items loaded");
                 task2.StopTask();

                 // Task 3: Load the Project work item
                 task3.StartTask();
                 task3.MaxValue = 1;
                 AnsiConsole.WriteLine($"Stage 3: Load the Project work item with ID {settings.projectId} from {configSettings.target.Organization} ");
                 AzureDevOpsApi targetApi = CreateAzureDevOpsConnection(settings.targetAccessToken, configSettings.target.Organization, configSettings.target.Project);
                 WorkItemFull projectItem = await targetApi.GetWorkItem((int)settings.projectId);
                 AnsiConsole.WriteLine($"Stage 3: Project `{projectItem.fields.SystemTitle}` loaded ");
                 task3.Increment(1);
                 task3.StopTask();

                 // Task 4: First Pass generation of Work Items to build
                 task4.MaxValue = jsonWorkItems.Count();
                 task4.StartTask();
                 AnsiConsole.WriteLine($"Stage 4: First Pass generation of Work Items to build will merge the provided json work items with the data from the template.");
                 List<WorkItemToBuild> buildItems = new List<WorkItemToBuild>();
                 await foreach (WorkItemToBuild witb in generateWorkItemsToBuildList(jsonWorkItems, templateWorkItems, projectItem, configSettings.target.Project))
                 {
                    // AnsiConsole.WriteLine($"Stage 4: processing {witb.guid}");
                     buildItems.Add(witb);
                     task4.Increment(1);
                 }
                 System.IO.File.WriteAllText($"{settings.OutputPath}\\WorkItemsToBuild-norelations.json", JsonConvert.SerializeObject(buildItems, Formatting.Indented));
                 task4.StopTask();
                 AnsiConsole.WriteLine($"Stage 4: Completed first pass.");

                 // Task 5: 
                 task5.MaxValue = jsonWorkItems.Count();
                 AnsiConsole.WriteLine($"Stage 5: Second Pass generate relations.");
                 task5.StartTask();
                 await foreach (WorkItemToBuild witb in generateWorkItemsToBuildRelations(buildItems, templateWorkItems))
                 {
                     // AnsiConsole.WriteLine($"Stage 5: processing {witb.guid} for output of {witb.relations.Count-1} relations");
                     task5.Increment(1);
                 }
                 System.IO.File.WriteAllText($"{settings.OutputPath}\\WorkItemsToBuild.json", JsonConvert.SerializeObject(buildItems, Formatting.Indented));
                 task5.StopTask();
                 AnsiConsole.WriteLine($"Stage 5: Completed second pass.");

             });



            ////second pass, add relations
            //foreach (var item in configWorkItems)
            //{
            //    WorkItemFull templateWorkItem = null;
            //    if (item.id != null)
            //    {
            //        templateWorkItem = templateApi.GetWorkItem((int)item.id).Result;
            //    }
            //    WorkItemToBuild newItem = buildItems.Find(x => x.templateId == item.id);

            //}



            ////newItem.relations = new List<WorkItemToBuildRelation>() {
            ////        new WorkItemToBuildRelation() { rel = "System.LinkTypes.Hierarchy-Reverse", guid = Guid.NewGuid() },
            ////        new WorkItemToBuildRelation() { rel = "System.LinkTypes.Dependency-Forward", guid = Guid.NewGuid() },
            ////        new WorkItemToBuildRelation() { rel = "System.LinkTypes.Dependency-Reverse", guid = Guid.NewGuid() }
            ////    };


            //System.IO.File.WriteAllText($"{settings.OutputPath}\\WorkItemsToBuild.json", JsonConvert.SerializeObject(buildItems, Formatting.Indented));


            return 0;
        }

        private async IAsyncEnumerable<WorkItemToBuild> generateWorkItemsToBuildList(List<jsonWorkItem> jsonWorkItems, List<WorkItemFull> templateWorkItems, WorkItemFull projectItem, string targetTeamProject)
        {
            foreach (var item in jsonWorkItems)
            {
                WorkItemFull templateWorkItem = null;
                if (item.id != null)
                {
                    templateWorkItem = templateWorkItems.Find(x => x.id == item.id);
                }
                WorkItemToBuild newItem = new WorkItemToBuild();
                newItem.guid = Guid.NewGuid();
                newItem.templateId = item.id;
                newItem.fields = new Dictionary<string, string>()
                {
                    { "System.Title", item.fields.title },
                    { "Custom.Product", item.fields.product },
                    { "System.Tags", string.Join(";" , item.tags, item.area, item.fields.product, templateWorkItem != null? templateWorkItem.fields.SystemTags : "") },
                    { "System.AreaPath", string.Join("\\", targetTeamProject, item.area)},
                    { "System.Description",  templateWorkItem != null? templateWorkItem.fields.SystemDescription: "" },
                    { "Microsoft.VSTS.Common.AcceptanceCriteria", templateWorkItem != null? templateWorkItem.fields.MicrosoftVSTSCommonAcceptanceCriteria: "" }
                };
                newItem.relations = new List<WorkItemToBuildRelation>() { new WorkItemToBuildRelation() { rel = "System.LinkTypes.Hierarchy-Reverse", targetId = projectItem.id } };
                yield return newItem;
            }
        }

        private async IAsyncEnumerable<WorkItemToBuild> generateWorkItemsToBuildRelations(List<WorkItemToBuild> workItemsToBuild, List<WorkItemFull> templateWorkItems)
        {
            foreach (WorkItemToBuild item in workItemsToBuild)
            {
                WorkItemFull templateWorkItem = null;
                if (item.templateId != null)
                {
                    templateWorkItem = templateWorkItems.Find(x => x.id == item.templateId);
                    foreach (var relation in templateWorkItem.relations)
                    {
                        // Skip parents
                        if (relation.rel == "System.LinkTypes.Hierarchy-Reverse") continue;
                        var templateIdToLinkTo = Int32.Parse(relation.url.Split('/').Last());
                        WorkItemToBuild workItemToBuildToLinkTo = workItemsToBuild.Find(x => x.templateId == templateIdToLinkTo);
                        if (workItemToBuildToLinkTo != null)
                        {
                            item.relations.Add(new WorkItemToBuildRelation() { rel = relation.rel, templateId = templateIdToLinkTo, targetId = 0, guid = workItemToBuildToLinkTo.guid });
                        } else
                        {
                            AnsiConsole.WriteLine($"Relation {relation.rel} to {templateIdToLinkTo} not found in work items to build.");
                        }                        
                    }
                }
                yield return item;
            }
        }


    }
}
