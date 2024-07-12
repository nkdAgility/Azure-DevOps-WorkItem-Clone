![Azure-DevOps-WorkItem-Clone](https://socialify.git.ci/nkdAgility/Azure-DevOps-WorkItem-Clone/image?description=1&descriptionEditable=Clone%20Work%20Items%20under%20Parent%20bassed%20on%20JSON%20and%20Template&forks=1&language=1&name=1&owner=1&pattern=Signal&stargazers=1&theme=Light)
[![Elmah.io](https://img.shields.io/badge/sponsored_by-elmah_io-0da58e)](https://elmah.io)
![GitHub Release](https://img.shields.io/github/v/release/nkdagility/Azure-DevOps-WorkItem-Clone)
![GitHub Release](https://img.shields.io/github/v/release/nkdagility/Azure-DevOps-WorkItem-Clone?include_prereleases)


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
  "inputJsonFile": "ADO_TESTProjPipline_V03.json",
  "targetAccessToken": null,
  "targetOrganization": "nkdagility-preview",
  "targetProject": "Clone-Demo",
  "targetParentId": 540,
  "templateAccessToken": null,
  "templateOrganization": "Clone-MO-ATE",
  "templateProject": "Clone Template"
}
 ```

 ## inputJsonFile Example

 ```json
 [
  {
    "templateId": 213928,
    "fields": [
      {"System.Title": "Technical specification"},
      {"Custom.Project": "CC000_000A01"},
      {"System.Tags": "Customer Document"},
      {"System.AreaPath": "#{targetProject}#\\TPL"}
    ]
  },
  {
    "templateId": 213928,
    "fields": [
      {"System.Title": "Technical specification"},
      {"Custom.Project": "CC000_000A02"},
      {"System.Tags": "Technical specification"},
      {"System.AreaPath": "#{targetProject}#\\TPL"}
    ]
  },
  }
]
```

