

namespace ABB.WorkItemClone.AzureDevOps.DataContracts
{
    public class WorkItemToBuild
    {
        public Guid guid { get;  set; }
        public int? templateId { get;  set; }
        public Dictionary<string, string> fields { get;  set; }
        public List<WorkItemToBuildRelation> relations { get;  set; }

    }
}