![Azure-DevOps-WorkItem-Clone](https://socialify.git.ci/nkdAgility/Azure-DevOps-WorkItem-Clone/image?description=1&descriptionEditable=Clone%20Work%20Items%20under%20Parent%20bassed%20on%20JSON%20and%20Template&forks=1&language=1&name=1&owner=1&pattern=Signal&stargazers=1&theme=Light)
# Azure DevOps WorkItem Clone

The purpose of this tool is to clone work items from a template project to a target project. The tool uses a JSON configuration file which it combines with a template in Azure DevOps to specify what work items to create and how to update them.

## Installation

Download the [latest release](https://github.com/nkdAgility/Azure-DevOps-WorkItem-Clone/releases/latest) from the [releases page](https://github.com/nkdAgility/Azure-DevOps-WorkItem-Clone/releases), unzip, and run `WorkItemClone.exe` with the appropriate parameters.

## Commands

### `clone`

Clones work items from a template project to a target project incorproating a JSON configuration file specificyng what work items to create and how to update them.

#### Parameters

*General Parameters* - These are general parameters that control the behaviour of the clone process.

 - `--config` - The path to the configuration file. Default is `.\configuration.json`. This can be used to had code paramiters... Command line params overrides the configuration file.
 - `--CachePath` - Folder used to cache data like the template. Default is `.\cache`.
 - `--inputJsonFile` - The path to the JSON file that instructs the creation of the work items
 - `--runname` - The name of the run. Default is the current DateTime. Use this to rerun a creation that failed or was interupted.

*Template Parameters* - The template contains the Descrition, Acceptance Criteria and relationship to other work itsm that we will clone.

 - `--templateAccessToken` - The access token for the template instance.
 - `--templaterganization` - The name of the organisation to clone work items to.
 - `--templateProject` - The name of the prject to clone work items from.
 
 *Target Parameters* - The target environemnt is where we will clone the work items to.

 - `--targetAccessToken` - The access token for the target project.
 - `--targetOrganization` - The name of the organisation to clone work items to.
 - `--targetProject` - The name of the prject to clone work items to.
 - `--targetParentId` - All cloned work items will be come a child of this work item

 *Target Query* - The target query is used to create a query in the target project to show the cloned work items.

 - `--targetQuery` - The query to create in the target project. Default is `SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AreaPath],[System.AssignedTo],[System.State] FROM workitems WHERE [System.Parent] = @projectID`.
 - `--targetQueryTitle` - The title of the query to create in the target project. Default is `Project-@RunName - @projectTitle`.
 - `--targetQueryFolder` - The folder to create the query in the target project. Default is `Shared Queries`.

 You can use the following parameters:
 
 - *@projectID* - The ID of the target parent item
 - *@projectTitle* - The title of the parent item
 - *@projectTags* - the tags of the item
 - *@RunName* - The name of the run

 *Optional Parameters* - These are optional parameters that can be used to control the behaviour of the clone process.

 - `--NonInteractive` - Disables interactive mode. Default is `false`.
 - `--ClearCache` - Clears the cache. Default is `false`.

 *Typical usage*:
 
 ```powershell
 clone --inputJsonFile ..\\..\\..\\..\\TestData\\ADO_TESTProjPipline_V03.json --targetParentId 540 --templateAccessToken tqvemdfaucsriu6e3uti7dya --targetAccessToken ay5xc2kn5i3xcsmw5fu65ja 
 ```

 #### Runs

 The consept of runs is to allow users to restart a failed or interupted run. The run name is used to identify the run and the cache is used to store the state of the run.

 The example below will create a subfolder to the cache called `Bob` where it will store the state of the run. If the run fails or is interupted you can restart the run by using the same run name. Rerunning the same run will not create duplicate work items and will not rebuild the output file that is generated in steps 4 and 5. It will reuse the existing one. If you need to change the input file then you will need to create a new run.

 When using the `--runname` parameter the `--inputJsonFile ` will not be used if a cache exists for the run. The input file will be read from the cache.

 *Typical usage*:
 
 ```powershell
  clone --runname Bob --inputJsonFile ..\\..\\..\\..\\TestData\\ADO_TESTProjPipline_V03.json --targetParentId 540 --templateAccessToken tqvemdfaucsriu6e3uti7dya --targetAccessToken ay5xc2kn5i3xcsmw5fu65ja 
 ```


 ### `init`

 Leads you through the process of creating a configuration file.

 #### Parameters

*General Parameters* - These are general parameters that control the behaviour of the clone process.

 - `--config` - The path to the configuration file. Default is `.\configuration.json`. This can be used to had code paramiters... Command line params overrides the configuration file.

 *Typical usage*:
 
 ```powershell
 init --config configuration.json
 ```

 ## Configuration Example

 ```json
{
  "CachePath": "./cache",
  "inputJsonFile": "TESTProjPipline_V03.json",
  "targetAccessToken": "************************************",
  "targetOrganization": "nkdagility-preview",
  "targetProject": "Clone-Demo",
  "targetParentId": 540,
  "templateAccessToken": "************************************",
  "templateOrganization": "orgname",
  "templateProject": "template Project",
  "templateParentId": 212315,
  "targetQuery": "SELECT [Custom.Product], [System.Title], [System.Description], [Custom.DeadlineDate], [System.AreaPath], [System.AssignedTo], [System.State], [Custom.Notes], [System.WorkItemType], [Custom.TRA_Milestone] FROM WorkItemLinks WHERE (Source.[System.Id] = @projectID or Source.[System.Parent] = @projectID) and ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward') and (Target.[System.Parent] = @projectID) ORDER BY [Custom.DeadlineDate] mode(Recursive)",
  "targetQueryTitle": "Project-@RunName - @projectTitle",
  "targetQueryFolder": "Shared Queries"
}
 ```

## Control File (--inputFile)

The control file consists of a list of work items that you want to create. Each work item has an `id` and a list of `fields`. 


- `id` - The `id` is the optional ID of a template item. If there is an ID, and there is a template item, then the tool will load that template and allow you to select values from it to add to the work item that you create. This template item will also be used to load relationships to other work items and if both ends of the relationship are part of the control file it will wire them up as expected. Not specifying an D wil result in a new work item based only on the control file.
- `fields` - These `fields` are the fields that will be used to create the work item. You can use any field `Refname` from the target Azure DevOps work item.

example:

 ```json
[
  {
    "id": 213928,
    "fields": {
      "System.AreaPath": "Engineering Group\\ECH Group\\ECH TPL 1",
      "System.Tags": "Customer Document",
      "System.Title": "Technical specification",
      "Custom.Product": "CC",
      "Microsoft.VSTS.Scheduling.Effort": 12,
      "Custom.TRA_Milestone": "E0.1"
    }
  },
  {
    "id": "",
    "fields": {
      "System.AreaPath": "Engineering Group\\ECH Group\\ECH TPL 1",
      "System.Tags": "",
      "System.Title": "E4.8 Assessment",
      "Custom.Product": "",
      "Microsoft.VSTS.Scheduling.Effort": 2,
      "Custom.TRA_Milestone": "E4.8"
    }
]
```

### Fields Manipulation

If you wish to pull fields from the template work item you can use the following syntax:

- `$[System.Description|template]` - this will pull the `System.Description` field from the template work item. Since template is the default you can also use `$[System.Description]`.
- `$[System.Description|control]` - this will pull the `System.Description` field from the control item data.

This would make the following valid:

 ```json
[
  {
    "id": 213928,
    "fields": {
      "System.AreaPath": "Engineering Group\\ECH Group\\ECH TPL 1",
      "System.Tags": "Customer Document",
      "System.Title": "Technical specification [ $[Custom.Product|control] ]",
      "Custom.Product": "CC",
      "Microsoft.VSTS.Scheduling.Effort": 12,
      "Custom.TRA_Milestone": "E0.1",
      "System.Description": "$[System.Description] for $[Custom.Product|control]",
      "Microsoft.VSTS.Common.AcceptanceCriteria": "$[Microsoft.VSTS.Common.AcceptanceCriteria]"
    }
  }
]
```

You can also specify `${fromtemplate}` or `${valuefromtemplate}` to pull just the value from the template that is the same name as the control field in focus.

 ```json
[
  {
    "id": 213928,
    "fields": {
      "System.AreaPath": "Engineering Group\\ECH Group\\ECH TPL 1",
      "System.Tags": "Customer Document",
      "System.Title": "Technical specification",
      "Custom.Product": "CC",
      "Microsoft.VSTS.Scheduling.Effort": 12,
      "Custom.TRA_Milestone": "E0.1",
      "System.Description": "The description: ${fromtemplate}",
      "Microsoft.VSTS.Common.AcceptanceCriteria": "${fromtemplate}"
    }
  }
]
```

