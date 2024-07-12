using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDevOps.WorkItemClone.DataContracts
{

    public class NodeClassification
    {
        public int id { get; set; }
        public string identifier { get; set; }
        public string name { get; set; }
        public string structureType { get; set; }
        public bool hasChildren { get; set; }
        public string path { get; set; }
        public NodeAttributes attributes { get; set; }
        public NodeLinks _links { get; set; }
        public string url { get; set; }
    }

    public class NodeAttributes
    {
        public DateTime startDate { get; set; }
        public DateTime finishDate { get; set; }
    }

    public class NodeLinks
    {
        public NodeSelf self { get; set; }
        public NodeParent parent { get; set; }
    }

    public class NodeSelf
    {
        public string href { get; set; }
    }

    public class NodeParent
    {
        public string href { get; set; }
    }

}
