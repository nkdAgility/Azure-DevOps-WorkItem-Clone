
namespace AzureDevOps.WorkItemClone.DataContracts
{
    public class WorkItemToBuildRelation
    {
        public WorkItemToBuildRelation()
        {
        }

        public string rel { get; set; }
        public Guid guid { get; set; }
        public int targetId { get; set; }
        public int templateId { get; set; }
    }
}