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
        public WorkItemFull? ItemFromtemplate { get; set; }
        public jsonWorkItem ItemFromConfig { get; set; }
    }

    public abstract class Operation
    {
        public string op { get; set; }
        public string path { get; set; }
        public object from { get; set; }
    }

    public class FieldOperation : Operation
    {
        public string value { get; set; }
    }

    public class RelationOperation : Operation
    {
    public RelationValue value { get; set; }
    }


    public class RelationValue
    {
        public string rel { get; set; }
        public string url { get; set; }
        public RelationAttributes attributes { get; set; }
    }

    public class RelationAttributes
    {
        public string comment { get; set; }
    }



}
