using AzureDevOps.WorkItemClone;
using AzureDevOps.WorkItemClone.DataContracts;
using AzureDevOps.WorkItemClone.ConsoleUI.DataContracts;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.VisualStudio.Services.CircuitBreaker;

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

            AnsiConsole.MarkupLine($"[red]Run: [/] {config.RunName}" );
            string runCache = $"{config.CachePath}\\{config.RunName}";
            DirectoryInfo outputPathInfo = CreateOutputPath(runCache);

            AzureDevOpsApi templateApi = CreateAzureDevOpsConnection(config.templateAccessToken, config.templateOrganization, config.templateProject);
            AzureDevOpsApi targetApi = CreateAzureDevOpsConnection(config.targetAccessToken, config.targetOrganization, config.targetProject);

            List<jsonWorkItem1> inputWorkItems = DeserializeWorkItemList(config);


            // --------------------------------------------------------------
            WriteOutSettings(config);
            string runConfig = $"{runCache}\\config.json";
            System.IO.File.WriteAllText(runConfig, JsonConvert.SerializeObject(config, Formatting.Indented));

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
                 var task4 = ctx.AddTask("[bold]Stage 4[/]: Create Output Plan", false);
                 var task5 = ctx.AddTask("[bold]Stage 5[/]: Create Output Plan Relations ", false);
                 var task6 = ctx.AddTask("[bold]Stage 6[/]: Create Work Items", false);

                 string cacheTemplateWorkItemsFile = $"{config.CachePath}\\templateCache-{config.templateOrganization}-{config.templateProject}-{config.templateParentId}.json";

                 if (config.ClearCache && System.IO.File.Exists(cacheTemplateWorkItemsFile))
                 {
                     System.IO.File.Delete(cacheTemplateWorkItemsFile);
                 }

                 task1.MaxValue = 1;
                 List<WorkItemFull> templateWorkItems = null;



                 task1.StartTask();
                 task2.StartTask();

                 if (System.IO.File.Exists(cacheTemplateWorkItemsFile))
                 {

                     var changedDate = System.IO.File.GetLastWriteTime(cacheTemplateWorkItemsFile).AddDays(1).Date;
                     //Test Cache
                     QueryResults fakeItemsFromTemplateQuery;
                     fakeItemsFromTemplateQuery = await templateApi.GetWiqlQueryResults("Select [System.Id] From WorkItems Where [System.TeamProject] = '@project' AND [System.Parent] = @id AND [System.ChangedDate] > '@changeddate' order by [System.CreatedDate] desc", new Dictionary<string, string>() { { "@id", config.templateParentId.ToString() }, { "@changeddate", changedDate.ToString("yyyy-MM-dd") } });
                     if (fakeItemsFromTemplateQuery.workItems.Length == 0)
                     {                    
                     AnsiConsole.WriteLine($"Stage 1: Checked template for changes. None Detected. Loading Cache");

                     // Load from Cache
                     
                     task1.Increment(1);
                     task1.Description = task1.Description + " (cache)";
                     await Task.Delay(250);
                     task1.StopTask();
                     //////////////////////
                     templateWorkItems = JsonConvert.DeserializeObject<List<WorkItemFull>>(System.IO.File.ReadAllText(cacheTemplateWorkItemsFile));
                     task2.Increment(templateWorkItems.Count);
                     task2.Description = task2.Description + " (cache)";
                     AnsiConsole.WriteLine($"Stage 2: Loaded {templateWorkItems.Count()} work items from cache.");
                     }
                 }

                 if (templateWorkItems == null)
                 {
                     // Get From Server
                     // --------------------------------------------------------------
                     // Task 1: query for template work items
                     task1.StartTask();
                     
                     //AnsiConsole.WriteLine("Stage 1: Executing items from Query");
                     QueryResults fakeItemsFromTemplateQuery;
                     fakeItemsFromTemplateQuery = await templateApi.GetWiqlQueryResults("Select [System.Id] From WorkItems Where [System.TeamProject] = '@project' AND [System.Parent] = @id order by [System.CreatedDate] desc", new Dictionary<string, string>() { { "@id", config.templateParentId.ToString() } });
                     AnsiConsole.WriteLine($"Stage 1: Query returned {fakeItemsFromTemplateQuery.workItems.Count()} items id's from the template.");
                     task1.Increment(1);
                     task1.StopTask();
                     // --------------------------------------------------------------
                     // Task 2: getting work items and their full data
                     task2.MaxValue = fakeItemsFromTemplateQuery.workItems.Count();
                     task2.StartTask();
                     await Task.Delay(250);
                     //AnsiConsole.WriteLine($"Stage 2: Starting process of {task2.MaxValue} work items to get their full data ");
                     templateWorkItems = new List<WorkItemFull>();
                     //AnsiConsole.WriteLine($"Stage 2: Loading {fakeItemsFromTemplateQuery.workItems.Count()} work items from template.");
                     await foreach (var workItem in templateApi.GetWorkItemsFullAsync(fakeItemsFromTemplateQuery.workItems))
                     {
                         //AnsiConsole.WriteLine($"Stage 2: Processing  {workItem.id}:`{workItem.fields.SystemTitle}`");
                         templateWorkItems.Add(workItem);
                         task2.Increment(1);
                     }
                     System.IO.File.WriteAllText(cacheTemplateWorkItemsFile, JsonConvert.SerializeObject(templateWorkItems, Formatting.Indented));
                     //AnsiConsole.WriteLine($"Stage 2: All {task2.MaxValue} work items loaded");
                     await Task.Delay(250);
                     task2.StopTask();
                 }
                 await Task.Delay(250);

                 // --------------------------------------------------------------
                 string targetProjectRunFile = $"{runCache}\\targetProject.json";
                 WorkItemFull projectItem;
                 if (System.IO.File.Exists(targetProjectRunFile))
                 {
                     task3.StartTask();
                     task3.MaxValue = 1;
                     // Load from Cache
                     projectItem = JsonConvert.DeserializeObject<WorkItemFull>(System.IO.File.ReadAllText(targetProjectRunFile));
                     task3.Increment(1);
                     await Task.Delay(250);
                     task3.StopTask();
                     task3.Description = task3.Description + " (run cache)";

                 }
                 else
                 {
                     // --------------------------------------------------------------
                     // Task 3: Load the Project work item
                     task3.StartTask();
                     await Task.Delay(250);
                     task3.MaxValue = 1;
                     projectItem = await targetApi.GetWorkItem((int)config.targetParentId);
                     System.IO.File.WriteAllText(targetProjectRunFile, JsonConvert.SerializeObject(projectItem, Formatting.Indented));
                     //AnsiConsole.WriteLine($"Stage 3: Project `{projectItem.fields.SystemTitle}` loaded ");
                     task3.Increment(1);
                     await Task.Delay(250);
                     task3.StopTask();
                     // --------------------------------------------------------------
                 }
                 await Task.Delay(250);
                 // --------------------------------------------------------------

                 string workItemsToBuildRunFile = $"{runCache}\\output.json";
                 List<WorkItemToBuild> buildItems;
                 if (System.IO.File.Exists(workItemsToBuildRunFile))
                 {
                     buildItems = JsonConvert.DeserializeObject<List<WorkItemToBuild>>(System.IO.File.ReadAllText(workItemsToBuildRunFile));
                     // Update lists
                     task4.MaxValue = 1;
                     task4.Increment(1);
                     task4.Description = task4.Description + " (run cache)";
                     task5.MaxValue = 1;
                     task5.Increment(1);
                     task5.Description = task5.Description + " (run cache)";
                 } else
                 {
                     // Task 4: First Pass generation of Work Items to build
                     task4.MaxValue = inputWorkItems.Count();
                     task4.StartTask();
                     await Task.Delay(250);
                     //AnsiConsole.WriteLine($"Stage 4: First Pass generation of Work Items to build will merge the provided json work items with the data from the template.");
                     buildItems = new List<WorkItemToBuild>();
                     await foreach (WorkItemToBuild witb in generateWorkItemsToBuildList(inputWorkItems, templateWorkItems, projectItem, config.targetProject))
                     {
                         // AnsiConsole.WriteLine($"Stage 4: processing {witb.guid}");
                         buildItems.Add(witb);
                         task4.Increment(1);
                     }
                     await Task.Delay(250);
                     task4.StopTask();
                     //AnsiConsole.WriteLine($"Stage 4: Completed first pass.");
                     // --------------------------------------------------------------
                     // Task 5: Second Pass Add Relations
                     task5.MaxValue = inputWorkItems.Count();
                     //AnsiConsole.WriteLine($"Stage 5: Second Pass generate relations.");
                     task5.StartTask();
                     await Task.Delay(250);
                     await foreach (WorkItemToBuild witb in generateWorkItemsToBuildRelations(buildItems, templateWorkItems))
                     {
                         //AnsiConsole.WriteLine($"Stage 5: processing {witb.guid} for output of {witb.relations.Count-1} relations");
                         task5.Increment(1);
                     }
                     System.IO.File.WriteAllText(workItemsToBuildRunFile, JsonConvert.SerializeObject(buildItems, Formatting.Indented));
                     await Task.Delay(250);
                     task5.StopTask();
                 }
                 await Task.Delay(250);
                 // --------------------------------------------------------------


                 //AnsiConsole.WriteLine($"Stage 5: Completed second pass.");

                 // --------------------------------------------------------------
                 // Task 6: Create work items in target
                 
                task6.MaxValue = buildItems.Count();
                int taskCount = 1;
                 AnsiConsole.WriteLine($"Processing {buildItems.Count()} items");
                 task6.Description = $"[bold]Stage 6[/]: Create Work Items (0/{buildItems.Count()})";
                 task6.StartTask();
                 await foreach ((WorkItemToBuild witb, string status, int skipped, int failed, int created) result in CreateWorkItemsToBuild(buildItems, projectItem, targetApi))
                 {
                     //AnsiConsole.WriteLine($"Stage 6: Processing {witb.guid} for output of {witb.relations.Count - 1} relations");
                     task6.Increment(1);
                     taskCount++;
                     task6.Description = $"[bold]Stage 6[/]: Create Work Items ({taskCount}/{buildItems.Count()} c:{result.created}, s:{result.skipped}, f:{result.failed})";
                     switch (result.status)
                     {
                         case "created":
                             //AnsiConsole.WriteLine($"Created {result.witb.guid}");
                             System.IO.File.WriteAllText(workItemsToBuildRunFile, JsonConvert.SerializeObject(buildItems, Formatting.Indented));
                             break;
                         case "skipped":
                             //AnsiConsole.WriteLine($"Skipped {result.witb.guid}");
                             await Task.Delay(50);
                             break;
                         case "failed":
                             AnsiConsole.WriteLine($"Failed {result.witb.guid}");
                             break;
                     }
                     
                 }
                 task6.StopTask();
                 //AnsiConsole.WriteLine($"Stage 6: All Work Items Created.");
             });


            AnsiConsole.WriteLine($"Complete...");

            return 0;
        }

        private async IAsyncEnumerable<WorkItemToBuild> generateWorkItemsToBuildList(List<jsonWorkItem1> jsonWorkItems, List<WorkItemFull> templateWorkItems, WorkItemFull projectItem, string targetTeamProject)
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
                newItem.workItemType = templateWorkItem != null ? templateWorkItem.fields.SystemWorkItemType : "Deliverable";
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

        private async IAsyncEnumerable<(WorkItemToBuild, string status, int skipped, int failed, int created)> CreateWorkItemsToBuild(List<WorkItemToBuild> workItemsToBuild, WorkItemFull projectItem, AzureDevOpsApi targetApi)
        {
            int skipped = 0;
            int failed = 0;
            int created = 0;
            foreach (WorkItemToBuild item in workItemsToBuild)
            {
                if (item.targetId != 0)
                {
                    skipped++;
                    yield return (item, "skipped", skipped, failed,created);
                } else
                {
                    WorkItemAdd itemToAdd = CreateWorkItemAddOperation(item, workItemsToBuild, projectItem);
                    WorkItemFull newWorkItem = await targetApi.CreateWorkItem(itemToAdd, item.workItemType);
                    if (newWorkItem != null)
                    {
                        created++;
                        item.targetUrl = newWorkItem.url;
                        item.targetId = newWorkItem.id;
                        yield return (item, "created", skipped, failed, created);
                    }
                    else
                    {
                        failed++;
                        yield return (item, "failed", skipped, failed, created);
                    }
                }                   
                
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
