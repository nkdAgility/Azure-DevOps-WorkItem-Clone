using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDevOps.WorkItemClone.DataContracts
{

    public class jsonWorkItem1
    {
        public int? id { get; set; }
        public string? area { get; set; }
        public string? tags { get; set; }
        public jsonFields1? fields { get; set; }
    }

    public class jsonFields1
    {
        public string? title { get; set; }
        public string? product { get; set; }
    }


}
