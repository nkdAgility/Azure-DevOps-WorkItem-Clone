using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.WorkItemClone.AzureDevOps.DataContracts
{

    public class WorkItemAdd
    {
        public List<Operation> Operations { get; set; } = new List<Operation>();
    }

    public class Operation
    {
        public string op { get; set; }
        public string path { get; set; }
        public object from { get; set; }
        public string value { get; set; }
    }

}
