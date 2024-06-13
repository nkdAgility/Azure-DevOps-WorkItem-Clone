using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.WorkItemClone.ConsoleUI.DataContracts
{
    public class ConfigurationSettings
    {
            public AzureDevOpsConnect template { get; set; }
            public AzureDevOpsConnect target { get; set; }
    }

    public class AzureDevOpsConnect
    {
        public string Organization { get; set; }
        public string Project { get; set; }
    }

}
