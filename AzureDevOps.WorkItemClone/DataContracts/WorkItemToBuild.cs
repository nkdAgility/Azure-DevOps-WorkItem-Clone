﻿

namespace AzureDevOps.WorkItemClone.DataContracts
{
    public class WorkItemToBuild
    {
        public Guid guid { get;  set; }
        public int? templateId { get;  set; }
        public Dictionary<string, string> fields { get;  set; }
        public List<WorkItemToBuildRelation> relations { get;  set; }
        public bool hasComplexRelation { get; set; }
        public string targetUrl { get; set; }
        public int targetId { get; set; }
        public string workItemType { get; set; }
    }
}