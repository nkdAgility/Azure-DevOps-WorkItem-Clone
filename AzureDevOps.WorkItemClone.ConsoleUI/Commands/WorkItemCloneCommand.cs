using AzureDevOps.WorkItemClone;
using AzureDevOps.WorkItemClone.DataContracts;
using AzureDevOps.WorkItemClone.ConsoleUI.DataContracts;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq;

namespace AzureDevOps.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemCloneCommand : WorkItemCommandBase<WorkItemCloneCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, WorkItemCloneCommandSettings settingsFromCmd)
        {
            WorkItemCloneCommandSettings config = null;
            if (File.Exists(settingsFromCmd.configFile))
            {
                    config = LoadWorkItemCloneCommandSettingsFromFile(settingsFromCmd.configFile);
            }
            if (config == null)
            {
                config = new WorkItemCloneCommandSettings();
            }
            CombineValuesFromConfigAndSettings(settingsFromCmd, config);

            DirectoryInfo outputPathInfo = CreateOutputPath(config.CachePath);
            AzureDevOpsApi templateApi = CreateAzureDevOpsConnection(config.templateAccessToken, config.templateOrganization, config.templateProject);
            List<jsonWorkItem> jsonWorkItems = LoadJsonFile(config.inputJsonFile);

            // --------------------------------------------------------------
            WriteOutSettings(config);
            if (!config.NonInteractive)
            {

                var proceedWithSettings = AnsiConsole.Prompt(
                new SelectionPrompt<bool> { Converter = value => value ? "Yes" : "No" }
                    .Title("Proceed with the aforementioned settings?")
                    .AddChoices(true, false)
            );
                if (!proceedWithSettings)
                {
                    AnsiConsole.MarkupLine("[red]Aborted[/]");
                    return 0;
                }
            }
            // --------------------------------------------------------------
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
                 var task4 = ctx.AddTask("[bold]Stage 4[/]: Plan Work Item Creation (First Pass)", false);
                 var task5 = ctx.AddTask("[bold]Stage 5[/]: Plan Work Item Creation (Second Pass) Relations ", false);
                 var task6 = ctx.AddTask("[bold]Stage 6[/]: Create Work Items", false);

                 // --------------------------------------------------------------
                 // Task 1: query for template work items
                 task1.StartTask();
                 task1.MaxValue = 1;
                 //AnsiConsole.WriteLine("Stage 1: Executing items from Query");
                 string cacheQueryFile = $"{config.CachePath}\\Step1-TemplateQuery.json";
                 if (config.ClearCache)
                 {
                     System.IO.File.Delete(cacheQueryFile);
                 }
                 QueryResults fakeItemsFromTemplateQuery;
                 if (System.IO.File.Exists(cacheQueryFile))
                 {
                     fakeItemsFromTemplateQuery = JsonConvert.DeserializeObject<QueryResults>(System.IO.File.ReadAllText(cacheQueryFile));
                     task1.Description = task1.Description + " (from cache)";
                     AnsiConsole.WriteLine($"Stage 1: Loaded {fakeItemsFromTemplateQuery.workItems.Count()} work items from cache.");
                 }
                 else
                 {
                     fakeItemsFromTemplateQuery = await templateApi.GetWiqlQueryResults();
                     AnsiConsole.WriteLine($"Stage 1: Query returned {fakeItemsFromTemplateQuery.workItems.Count()} items id's from the template.");
                     System.IO.File.WriteAllText($"{config.CachePath}\\Step1-TemplateQuery.json", JsonConvert.SerializeObject(fakeItemsFromTemplateQuery, Formatting.Indented));
                 }
                 task1.Increment(1);
                 task1.StopTask();
                 // --------------------------------------------------------------
                 // Task 2: getting work items and their full data
                 task2.MaxValue = fakeItemsFromTemplateQuery.workItems.Count();
                 task2.StartTask();
                 //AnsiConsole.WriteLine($"Stage 2: Starting process of {task2.MaxValue} work items to get their full data ");
                 string cachetemplateWorkItemsFile = $"{config.CachePath}\\Step2-TemplateItems.json";
                 if (config.ClearCache)
                 {
                     System.IO.File.Delete(cachetemplateWorkItemsFile);
                 }
                 List<WorkItemFull> templateWorkItems;
                 if (System.IO.File.Exists(cachetemplateWorkItemsFile))
                 {
                     templateWorkItems = JsonConvert.DeserializeObject<List<WorkItemFull>>(System.IO.File.ReadAllText(cachetemplateWorkItemsFile));
                     task2.Increment(templateWorkItems.Count);
                     task2.Description = task2.Description + " (from cache)";
                     AnsiConsole.WriteLine($"Stage 2: Loaded {templateWorkItems.Count()} work items from cache.");
                 }
                 else
                 {
                     templateWorkItems = new List<WorkItemFull>();
                     //AnsiConsole.WriteLine($"Stage 2: Loading {fakeItemsFromTemplateQuery.workItems.Count()} work items from template.");
                     await foreach (var workItem in templateApi.GetWorkItemsFullAsync(fakeItemsFromTemplateQuery.workItems))
                     {
                         //AnsiConsole.WriteLine($"Stage 2: Processing  {workItem.id}:`{workItem.fields.SystemTitle}`");
                         templateWorkItems.Add(workItem);
                         task2.Increment(1);
                     }
                     System.IO.File.WriteAllText($"{config.CachePath}\\Step2-TemplateItems.json", JsonConvert.SerializeObject(templateWorkItems, Formatting.Indented));
                 }

                 //AnsiConsole.WriteLine($"Stage 2: All {task2.MaxValue} work items loaded");
                 task2.StopTask();
                 // --------------------------------------------------------------
                 // Task 3: Load the Project work item
                 task3.StartTask();
                 task3.MaxValue = 1;
                 //AnsiConsole.WriteLine($"Stage 3: Load the Project work item with ID {config.targetParentId} from {config.targetOrganization} ");
                 AzureDevOpsApi targetApi = CreateAzureDevOpsConnection(config.targetAccessToken, config.targetOrganization, config.targetProject);
                 WorkItemFull projectItem = await targetApi.GetWorkItem((int)config.targetParentId);
                 System.IO.File.WriteAllText($"{config.CachePath}\\Step3-Project.json", JsonConvert.SerializeObject(projectItem, Formatting.Indented));
                 //AnsiConsole.WriteLine($"Stage 3: Project `{projectItem.fields.SystemTitle}` loaded ");
                 task3.Increment(1);
                 task3.StopTask();
                 // --------------------------------------------------------------
                 // Task 4: First Pass generation of Work Items to build
                 task4.MaxValue = jsonWorkItems.Count();
                 task4.StartTask();
                 //AnsiConsole.WriteLine($"Stage 4: First Pass generation of Work Items to build will merge the provided json work items with the data from the template.");
                 List<WorkItemToBuild> buildItems = new List<WorkItemToBuild>();
                 await foreach (WorkItemToBuild witb in generateWorkItemsToBuildList(jsonWorkItems, templateWorkItems, projectItem, config.targetProject))
                 {
                     // AnsiConsole.WriteLine($"Stage 4: processing {witb.guid}");
                     buildItems.Add(witb);
                     task4.Increment(1);
                 }
                 System.IO.File.WriteAllText($"{config.CachePath}\\Step4-WorkItemsToBuild.json", JsonConvert.SerializeObject(buildItems, Formatting.Indented));
                 task4.StopTask();
                 //AnsiConsole.WriteLine($"Stage 4: Completed first pass.");
                 // --------------------------------------------------------------
                 // Task 5: Second Pass Add Relations
                 task5.MaxValue = jsonWorkItems.Count();
                 //AnsiConsole.WriteLine($"Stage 5: Second Pass generate relations.");
                 task5.StartTask();
                 await foreach (WorkItemToBuild witb in generateWorkItemsToBuildRelations(buildItems, templateWorkItems))
                 {
                     //AnsiConsole.WriteLine($"Stage 5: processing {witb.guid} for output of {witb.relations.Count-1} relations");
                     task5.Increment(1);
                 }
                 System.IO.File.WriteAllText($"{config.CachePath}\\Step5-WorkItemsToBuild.json", JsonConvert.SerializeObject(buildItems, Formatting.Indented));
                 task5.StopTask();
                 //AnsiConsole.WriteLine($"Stage 5: Completed second pass.");

                 // --------------------------------------------------------------
                 // Task 6: Create work items in target
                 task6.MaxValue = buildItems.Count();
                 //AnsiConsole.WriteLine($"Stage 6: Create Work Items in Target.");
                 task6.StartTask();
                 await foreach (WorkItemToBuild witb in CreateWorkItemsToBuild(buildItems, projectItem, targetApi))
                 {
                     //AnsiConsole.WriteLine($"Stage 6: Processing {witb.guid} for output of {witb.relations.Count - 1} relations");
                     task6.Increment(1);
                 }
                 System.IO.File.WriteAllText($"{config.CachePath}\\Step6-WorkItemsToBuild.json", JsonConvert.SerializeObject(buildItems, Formatting.Indented));
                 task6.StopTask();
                 //AnsiConsole.WriteLine($"Stage 6: All Work Items Created.");


             });


            AnsiConsole.WriteLine($"Complete...");

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
                newItem.hasComplexRelation = false;
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
                            item.hasComplexRelation = true;
                        }
                        else
                        {
                            //AnsiConsole.WriteLine($"Relation {relation.rel} to {templateIdToLinkTo} not found in work items to build.");
                        }
                    }
                }
                yield return item;
            }
        }

        private async IAsyncEnumerable<WorkItemToBuild> CreateWorkItemsToBuild(List<WorkItemToBuild> workItemsToBuild, WorkItemFull projectItem, AzureDevOpsApi targetApi)
        {
            foreach (WorkItemToBuild item in workItemsToBuild)
            {
                if (item.targetId != 0) continue;
                WorkItemAdd itemToAdd = CreateWorkItemAddOperation(item, workItemsToBuild, projectItem);
                WorkItemFull newWorkItem = await targetApi.CreateWorkItem(itemToAdd, "Dependancy");
                if (newWorkItem == null)
                {
                    throw new Exception("Failed to create work item");
                }
                item.targetUrl = newWorkItem.url;
                item.targetId = newWorkItem.id;
                yield return item;
            }

        }

        private WorkItemAdd CreateWorkItemAddOperation(WorkItemToBuild item, List<WorkItemToBuild> workItemsToBuild, WorkItemFull projectItem)
        {
            WorkItemAdd itemAdd = new WorkItemAdd();

            foreach (var field in item.fields)
            {
                if (field.Value != null)
                {
                    itemAdd.Operations.Add(new FieldOperation() { op = "add", path = $"/fields/{field.Key}", value = field.Value });
                }
            }
            foreach (var relation in item.relations)
            {
                if (relation.rel == "System.LinkTypes.Hierarchy-Reverse")
                {
                    itemAdd.Operations.Add(new RelationOperation() { op = "add", path = "/relations/-", value = new RelationValue { rel = relation.rel, url = projectItem.url } });
                }
                else
                {
                    var targetItem = workItemsToBuild.Find(x => x.guid == relation.guid);
                    if (targetItem.targetUrl == null)
                    {
                        //AnsiConsole.WriteLine($"SKIP: Relation on {item.guid} does not yet exist. This relation should be added from the other side when {relation.guid} is processed.");
                    }
                    else
                    {
                        itemAdd.Operations.Add(new RelationOperation() { op = "add", path = "/relations/-", value = new RelationValue { rel = relation.rel, url = targetItem.targetUrl } });
                    }

                }
            }
            return itemAdd;
        }
    }
}
