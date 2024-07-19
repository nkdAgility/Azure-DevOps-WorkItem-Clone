using AzureDevOps.WorkItemClone;
using AzureDevOps.WorkItemClone.DataContracts;
using AzureDevOps.WorkItemClone.ConsoleUI.DataContracts;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Linq;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.VisualStudio.Services.CircuitBreaker;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Diagnostics.Eventing.Reader;
using AzureDevOps.WorkItemClone.Repositories;
using YamlDotNet.Serialization;

namespace AzureDevOps.WorkItemClone.ConsoleUI.Commands
{
    internal class WorkItemCloneCommand : WorkItemCommandBase<WorkItemCloneCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, WorkItemCloneCommandSettings settingsFromCmd)
        {
            if (!FileStoreCheckExtensionMatchesFormat(settingsFromCmd.configFile, settingsFromCmd.ConfigFormat))
            {
                AnsiConsole.MarkupLine($"[bold red]The file extension of {settingsFromCmd.configFile} does not match the format {settingsFromCmd.ConfigFormat.ToString()} selected! Please rerun with the correct format You can use --configFormat JSON or update your file to YAML[/]");
                return -1;
            }
            WorkItemCloneCommandSettings config = null;
            if (FileStoreExist(settingsFromCmd.configFile, settingsFromCmd.ConfigFormat))
            {
                config = FileStoreLoad<WorkItemCloneCommandSettings>(settingsFromCmd.configFile, settingsFromCmd.ConfigFormat);
            }
            if (config == null)
            {
                config = new WorkItemCloneCommandSettings();
            }
            CombineValuesFromConfigAndSettings(settingsFromCmd, config);

            AnsiConsole.MarkupLine($"[red]Run: [/] {config.RunName}");
            string runCache = $"{config.CachePath}\\{config.RunName}";
            DirectoryInfo outputPathInfo = CreateOutputPath(runCache);
            AzureDevOpsApi targetApi = CreateAzureDevOpsConnection(config.targetAccessToken, config.targetOrganization, config.targetProject);

            JArray workItemControlList = DeserializeControlFile(config);

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
                 var task1 = ctx.AddTask("[bold]Stage 1+2[/]: Load Template Items", false);
                 var task3 = ctx.AddTask("[bold]Stage 3[/]: Get Target Project", false);
                 var task4 = ctx.AddTask("[bold]Stage 4[/]: Create Output Plan", false);
                 var task5 = ctx.AddTask("[bold]Stage 5[/]: Create Output Plan Relations ", false);
                 //var task51 = ctx.AddTask("[bold]Stage 5.1[/]: Validate Data ", false);
                 var task6 = ctx.AddTask("[bold]Stage 6[/]: Create Work Items", false);
                 var task7 = ctx.AddTask("[bold]Stage 7[/]: Create Query", false);

                 string cacheTemplateWorkItemsFile = $"{config.CachePath}\\templateCache-{config.templateOrganization}-{config.templateProject}-{config.templateParentId}.json";

                 if (config.ClearCache && System.IO.File.Exists(cacheTemplateWorkItemsFile))
                 {
                     System.IO.File.Delete(cacheTemplateWorkItemsFile);
                 }

                 task1.StartTask();
                 IWorkItemRepository templateWor = new WorkItemRepository(config.CachePath, config.templateOrganization, config.templateProject, config.templateAccessToken, (int)config.templateParentId);
                 await foreach (var result in templateWor.GetWorkItemsFullAsync())
                 {
                     //AnsiConsole.WriteLine($"Stage 2: Processing  {workItem.id}:`{workItem.fields.SystemTitle}`");
                     task1.MaxValue = result.total;
                     if (result.total == result.processed)
                     {
                         task1.Increment(result.processed);
                         await Task.Delay(250);
                     }
                     task1.Description = $"[bold]Stage 1+2[/]: Load Template Items ({result.loadingFrom})";
                     task1.Increment(1);
                 }
                 task1.StopTask();

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
                 }
                 else
                 {
                     // Task 4: First Pass generation of Work Items to build
                     task4.MaxValue = workItemControlList.Count();
                     task4.StartTask();
                     await Task.Delay(250);
                     //AnsiConsole.WriteLine($"Stage 4: First Pass generation of Work Items to build will merge the provided json work items with the data from the template.");
                     buildItems = new List<WorkItemToBuild>();
                     await foreach (WorkItemToBuild witb in generateWorkItemsToBuildList(workItemControlList, templateWor.Data.workitems, projectItem, config.targetProject))
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
                     task5.MaxValue = workItemControlList.Count();
                     //AnsiConsole.WriteLine($"Stage 5: Second Pass generate relations.");
                     task5.StartTask();
                     await Task.Delay(250);
                     await foreach (WorkItemToBuild witb in generateWorkItemsToBuildRelations(buildItems, templateWor.Data.workitems))
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
                     task6.Description = $"[bold]Stage 6[/]: Create Work Items ({taskCount-1}/{buildItems.Count()} c:{result.created}, s:{result.skipped}, f:{result.failed})";
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


                 // --------------------------------------------------------------
                 // Task 7: Create Query
                 task7.MaxValue = 1;
                 task7.StartTask();

                 Dictionary<string, string> queryParameters = new Dictionary<string, string>() 
                 { 
                     { "@projectID", projectItem.id.ToString() }, 
                     { "@projectTitle", projectItem.fields.SystemTitle }, 
                     { "@projectTags", projectItem.fields.SystemTags },
                     { "@RunName", config.RunName }
                 };
                 var query = await targetApi.CreateProjectQuery(config.targetQueryTitle, config.targetQueryFolder, config.targetQuery, queryParameters);
                 task7.Increment(1);
                 task7.StopTask();



             });

           


            AnsiConsole.WriteLine($"Complete...");

            return 0;
        }

        private async IAsyncEnumerable<WorkItemToBuild> generateWorkItemsToBuildList(JArray jsonWorkItems, List<WorkItemFull> templateWorkItems, WorkItemFull projectItem, string targetTeamProject)
        {
            foreach (var item in jsonWorkItems)
            {
                WorkItemFull templateWorkItem = null;
                int jsonItemTemplateId = 0;
                if (int.TryParse(item["id"].Value<string>(), out jsonItemTemplateId))
                {
                    templateWorkItem = templateWorkItems.Find(x => x.id == jsonItemTemplateId);
                }
                WorkItemToBuild newItem = new WorkItemToBuild();
                newItem.guid = Guid.NewGuid();
                newItem.hasComplexRelation = false;
                newItem.templateId = jsonItemTemplateId;
                newItem.workItemType = templateWorkItem != null ? templateWorkItem.fields.SystemWorkItemType : "Deliverable";
                newItem.fields = new Dictionary<string, string>();
                newItem.fields.Add("System.Description", templateWorkItem != null ? templateWorkItem.fields.SystemDescription : "");
                newItem.fields.Add("Microsoft.VSTS.Common.AcceptanceCriteria", templateWorkItem != null ? templateWorkItem.fields.MicrosoftVSTSCommonAcceptanceCriteria : "");
                //{
                //    { "System.Tags", string.Join(";" , item.tags, item.area, item.fields.product, templateWorkItem != null? templateWorkItem.fields.SystemTags : "") },
                //    { "System.AreaPath", string.Join("\\", targetTeamProject, item.area)},
                //};
                var fields = item["fields"].ToObject<Dictionary<string, string>>();
                foreach (var field in fields)
                {
                    switch (field.Key)
                    {
                        case "System.AreaPath":
                            newItem.fields.Add(field.Key, string.Join("\\", targetTeamProject, field.Value));
                            break;
                        default:
                            if (newItem.fields.ContainsKey(field.Key))
                            {
                                newItem.fields[field.Key] = field.Value;
                            }
                            else
                            {
                                newItem.fields.Add(field.Key, field.Value);

                            }
                            break;
                    }
                }
                newItem.relations = new List<WorkItemToBuildRelation>() { new WorkItemToBuildRelation() { rel = "System.LinkTypes.Hierarchy-Reverse", targetId = projectItem.id } };
                yield return newItem;
            }
        }

        private async IAsyncEnumerable<WorkItemToBuild> generateWorkItemsToBuildRelations(List<WorkItemToBuild> workItemsToBuild, List<WorkItemFull> templateWorkItems)
        {
            foreach (WorkItemToBuild item in workItemsToBuild)
            {
                WorkItemFull templateWorkItem = null;
                if (item.templateId != 0)
                {
                    templateWorkItem = templateWorkItems.Find(x => x.id == item.templateId);
                    if (templateWorkItem != null)
                    { 
                    foreach (var relation in templateWorkItem?.relations)
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
                    yield return (item, "skipped", skipped, failed, created);
                }
                else
                {
                    WorkItemAdd itemToAdd = CreateWorkItemAddOperation(item, workItemsToBuild, projectItem);

                    if (!await ValidateOperations(targetApi, item, itemToAdd))
                    {
                        AnsiConsole.WriteLine($"[SKIP] {item.guid} As it does not pass field validation. issues listed above..");
                        skipped++;
                        yield return (item, "skipped", skipped, failed, created);
                        continue;
                    }

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

        
        internal Dictionary<string, bool> foundAreaPaths = new Dictionary<string, bool>();
        private async Task<bool> ValidateOperations(AzureDevOpsApi targetApi, WorkItemToBuild item, WorkItemAdd itemToAdd)
        {
            bool valid = true;
            foreach (FieldOperation operation in itemToAdd.Operations.FindAll(p => p is FieldOperation))
            {
                valid = valid & await CheckFieldExists(targetApi, item, valid, operation);
                switch (operation.path)
                {
                    case "/fields/System.AreaPath":
                        valid = valid & await CheckAreaPathExists(targetApi, item, valid, operation);
                        break;
                }
            }
            return valid;
        }

        private async Task<bool> CheckAreaPathExists(AzureDevOpsApi targetApi, WorkItemToBuild item, bool valid, FieldOperation operation)
        {
            if (!foundAreaPaths.ContainsKey(operation.value))
            {
                NodeClassification node = await targetApi.GetNodeClassification(operation.value.Replace($"{targetApi.Project}\\", ""));
                if (node == null)
                {
                    AnsiConsole.WriteLine($"[VALIDATE] {item.guid} has an invalid area path of {operation.value}. This is required to create a work item.");
                    valid = false;
                    foundAreaPaths.Add(operation.value, false);
                }
                else
                {
                    foundAreaPaths.Add(operation.value, true);
                }
            }
            else
            {
                if (!foundAreaPaths[operation.value])
                {
                    valid = false;
                }
            }

            return valid;
        }

        internal Dictionary<string, WorkItemFieldList> fieldsForTypes = new Dictionary<string, WorkItemFieldList>();
        internal Dictionary<string, bool> foundFields = new Dictionary<string, bool>();
        private async Task<bool> CheckFieldExists(AzureDevOpsApi targetApi, WorkItemToBuild item, bool valid, FieldOperation operation)
        {
            WorkItemFieldList fieldsLookup = null;
            if (fieldsForTypes.ContainsKey(item.workItemType))
            {
                fieldsLookup = fieldsForTypes[item.workItemType];
            }
            else
            {
                fieldsLookup = await targetApi.GetFieldsOnWorkItem(item.workItemType);
                fieldsForTypes.Add(item.workItemType, fieldsLookup);
            }
            int idx = operation.path.LastIndexOf('/');
            string referenceName = operation.path.Substring(idx + 1);
            string uniqueFieldkey = $"{item.workItemType}{operation.path}";

            if (foundFields.ContainsKey(uniqueFieldkey))
            {
                valid = valid && foundFields[uniqueFieldkey];
            }
            else
            {
                var foundField = fieldsLookup.value.FirstOrDefault(x => x.referenceName == referenceName);
                if (foundField == null)
                {
                    AnsiConsole.WriteLine($"[VALIDATE] Field {operation.path} does not exist on {item.workItemType} This is required to create a work item.");
                    valid = false;
                    foundFields.Add(uniqueFieldkey, false);
                }
                else
                {
                    foundFields.Add(uniqueFieldkey, true);
                }
            }

            return valid;
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
