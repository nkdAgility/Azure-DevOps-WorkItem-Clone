using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDevOps.WorkItemClone.DataContracts
{

    public class FieldItem
    {
        public string helpText { get; set; }
        public bool alwaysRequired { get; set; }
        public object defaultValue { get; set; }
        public string referenceName { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

}
