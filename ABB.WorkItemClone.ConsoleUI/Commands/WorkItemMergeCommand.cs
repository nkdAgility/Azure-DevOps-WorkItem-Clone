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


            var fakeItemsFromTemplateQuery = await AnsiConsole
               .Status()
               .StartAsync(
                   "Getting template items by query...",
                   _ => templateApi.GetWiqlQueryResults());

            if (!settings.NonInteractive)
            {
                var proceedWithTenplateImport = AnsiConsole
              .Prompt(
                  new SelectionPrompt<bool> { Converter = value => value ? "Yes" : "No" }
                      .Title($"Found {fakeItemsFromTemplateQuery.workItems.Count()} template items to import. Proceed?")
                      .AddChoices(true, false));

                if (!proceedWithTenplateImport)
                {
                    return 0;
                }
            }

            List<WorkItemFull> templateWorkItems = await AnsiConsole
                         .Progress()
                         .StartAsync(async ctx =>
                         {
                             var migrationTask = ctx.AddTask(
                                 "Loading Template Items...",
                                 maxValue: fakeItemsFromTemplateQuery.workItems.Count());

                             var successes = 0;
                             var failures = 0;

                             List<WorkItemFull> workItems = new List<WorkItemFull>();

                             await foreach (var workItem in templateApi.GetWorkItemsFullAsync(fakeItemsFromTemplateQuery.workItems))
                             {
                                 workItems.Add(workItem);
                                 migrationTask.Increment(1);

                             }

                             return workItems;
                         });


            if (!settings.NonInteractive)
            {
                var proceedWithMerge = AnsiConsole
              .Prompt(
                  new SelectionPrompt<bool> { Converter = value => value ? "Yes" : "No" }
                      .Title($"We will merge {jsonWorkItems.Count} json items with {fakeItemsFromTemplateQuery.workItems.Count()} template items. Proceed?")
                      .AddChoices(true, false));

                if (!proceedWithMerge)
                {
                    return 0;
                }
            }

            AzureDevOpsApi targetApi = CreateAzureDevOpsConnection(settings.targetAccessToken, configSettings.target.Organization, configSettings.target.Project);
            WorkItemFull projectItem = await AnsiConsole
               .Status()
               .StartAsync("Getting project item from target...",_ => targetApi.GetWorkItem((int)settings.projectId));

            // First pass to create work items.

            List<WorkItemToBuild> buildItems = await AnsiConsole
                        .Progress()
                        .StartAsync(async ctx =>
                        {
                            var migrationTask = ctx.AddTask(
                                "Creating Build items...",
                                maxValue: jsonWorkItems.Count());

                            var successes = 0;
                            var failures = 0;

                            List<WorkItemToBuild> buildItems = new List<WorkItemToBuild>();

                            await foreach (WorkItemToBuild witb in generateWorkItemsToBuildList(jsonWorkItems, templateWorkItems, projectItem, configSettings.target.Project))
                            {
                                buildItems.Add(witb);
                                migrationTask.Increment(1);

                            }

                            return buildItems;
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


    }
}
