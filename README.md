# abb-workitem-clone


## Commands

### `clone`

Clones work items from a template project to a target project incorproating a JSON configuration file specificyng what work items to create and how to update them.

 - `--outputPath`` - The path to the output folder. Default is `.\`.
 - `--jsonFile` - The path to the JSON configuration file. 
 - `--templateAccessToken` - The access token for the template project.
 - `--config` - The path to the configuration file. Default is `.\configuration.json`.
 - `--targetAccessToken` - The access token for the target project.
 - `--projectId` - The ID of the project to clone work items to.
 - `--NonInteractive` - Disables interactive mode. Default is `false`.
 - `--ClearCache` - Clears the cache. Default is `false`.

 Typical usage:
 
 ```powershell
 clone --outputPath ..\\..\\..\\..\\TestData\ --jsonFile ..\\..\\..\\..\\TestData\\ADO_TESTProjPipline_V03.json --projectId 540 --templateAccessToken tqvemdfaucsriu6e3uti7dya --targetAccessToken ay5xc2kn5i3xcsmw5fu65ja 
 ```

 ## Configuration

 ```json
 {
  "template": {
    "Organization": "orgname",
    "Project": "projectname"
  },
  "target": {
    "Organization": "orgname",
    "Project": "projectname"
  }
  }
 ```

 ## JSON Configuration Input

 ```json
 [
  {
    "id": 213928,
    "area": "TPL",
    "tags": "Customer Document",
    "fields": {
      "title": "Technical specification",
      "product": "CC000_000A01"
    }
  },
  {
    "id": 213928,
    "area": "TPL",
    "tags": "Customer Document",
    "fields": {
      "title": "Technical specification",
      "product": "CC000_000A02"
    }
  }
]
```

proposed new format not yet adopted:


 ```json
 [
  {
    "templateId": 213928,
    "fields": [
      {
        "System.Title": "Technical specification"
      },
      {
        "Custom.Project": "CC000_000A01"
      },
      {
        "System.Tags": "Customer Document"
      },
      {
        "System.AreaPath": "#{targetProject}#\\TPL"
      }
    ]
  },
  {
    "templateId": 213928,
    "fields": [
      {
        "System.Title": "Technical specification"
      },
      {
        "Custom.Project": "CC000_000A02"
      },
      {
        "System.Tags": "Technical specification"
      },
      {
        "System.AreaPath": "#{targetProject}#\\TPL"
      }
    ]
  },
  }
]
```

