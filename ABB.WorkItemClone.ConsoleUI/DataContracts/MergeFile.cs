using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.WorkItemClone.ConsoleUI.DataContracts
{

    public class MergeWorkItem
    {
        public int? id { get; set; }
        public string? area { get; set; }
        public string? tags { get; set; }
        public MergeFields? fields { get; set; }
    }

    public class MergeFields
    {
        public string? title { get; set; }
        public string? product { get; set; }
    }


}
