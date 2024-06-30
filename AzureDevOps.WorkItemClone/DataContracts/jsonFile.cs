using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.WorkItemClone.AzureDevOps.DataContracts
{

    public class jsonWorkItem
    {
        public int? id { get; set; }
        public string? area { get; set; }
        public string? tags { get; set; }
        public jsonFields? fields { get; set; }
    }

    public class jsonFields
    {
        public string? title { get; set; }
        public string? product { get; set; }
    }


}
