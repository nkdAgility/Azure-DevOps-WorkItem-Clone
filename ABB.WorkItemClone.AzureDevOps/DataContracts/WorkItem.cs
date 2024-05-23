using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.WorkItemClone.AzureDevOps.DataContracts
{


    public class WorkItemFull
    {
        public int id { get; set; }
        public int rev { get; set; }
        public Fields fields { get; set; }
        public Relation[] relations { get; set; }
        public _Links3 _links { get; set; }
        public string url { get; set; }
    }

    public class Fields
    {
        public int SystemId { get; set; }
        public int SystemAreaId { get; set; }
        public string SystemAreaPath { get; set; }
        public string SystemTeamProject { get; set; }
        public string SystemNodeName { get; set; }
        public string SystemAreaLevel1 { get; set; }
        public int SystemRev { get; set; }
        public DateTime SystemAuthorizedDate { get; set; }
        public DateTime SystemRevisedDate { get; set; }
        public int SystemIterationId { get; set; }
        public string SystemIterationPath { get; set; }
        public string SystemIterationLevel1 { get; set; }
        public string SystemWorkItemType { get; set; }
        public string SystemState { get; set; }
        public string SystemReason { get; set; }
        public DateTime SystemCreatedDate { get; set; }
        public SystemCreatedby SystemCreatedBy { get; set; }
        public DateTime SystemChangedDate { get; set; }
        public SystemChangedby SystemChangedBy { get; set; }
        public SystemAuthorizedas SystemAuthorizedAs { get; set; }
        public int SystemPersonId { get; set; }
        public int SystemWatermark { get; set; }
        public int SystemCommentCount { get; set; }
        public string SystemTitle { get; set; }
        public string SystemBoardColumn { get; set; }
        public bool SystemBoardColumnDone { get; set; }
        public DateTime MicrosoftVSTSCommonStateChangeDate { get; set; }
        public float MicrosoftVSTSCommonBacklogPriority { get; set; }
        public bool WEF_81B31D964B424A4FBC1D421C7BFD0CFA_SystemExtensionMarker { get; set; }
        public string WEF_81B31D964B424A4FBC1D421C7BFD0CFA_KanbanColumn { get; set; }
        public bool WEF_81B31D964B424A4FBC1D421C7BFD0CFA_KanbanColumnDone { get; set; }
        public string SystemDescription { get; set; }
        public string MicrosoftVSTSCommonAcceptanceCriteria { get; set; }
        public string SystemTags { get; set; }
        public int SystemParent { get; set; }
    }

    public class SystemCreatedby
    {
        public string displayName { get; set; }
        public string url { get; set; }
        public _Links _links { get; set; }
        public string id { get; set; }
        public string uniqueName { get; set; }
        public string imageUrl { get; set; }
        public string descriptor { get; set; }
    }

    public class _Links
    {
        public Avatar avatar { get; set; }
    }

    public class Avatar
    {
        public string href { get; set; }
    }

    public class SystemChangedby
    {
        public string displayName { get; set; }
        public string url { get; set; }
        public _Links1 _links { get; set; }
        public string id { get; set; }
        public string uniqueName { get; set; }
        public string imageUrl { get; set; }
        public string descriptor { get; set; }
    }

    public class _Links1
    {
        public Avatar1 avatar { get; set; }
    }

    public class Avatar1
    {
        public string href { get; set; }
    }

    public class SystemAuthorizedas
    {
        public string displayName { get; set; }
        public string url { get; set; }
        public _Links2 _links { get; set; }
        public string id { get; set; }
        public string uniqueName { get; set; }
        public string imageUrl { get; set; }
        public string descriptor { get; set; }
    }

    public class _Links2
    {
        public Avatar2 avatar { get; set; }
    }

    public class Avatar2
    {
        public string href { get; set; }
    }

    public class _Links3
    {
        public Self self { get; set; }
        public Workitemupdates workItemUpdates { get; set; }
        public Workitemrevisions workItemRevisions { get; set; }
        public Workitemcomments workItemComments { get; set; }
        public Html html { get; set; }
        public Workitemtype workItemType { get; set; }
        public Fields1 fields { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
    }

    public class Workitemupdates
    {
        public string href { get; set; }
    }

    public class Workitemrevisions
    {
        public string href { get; set; }
    }

    public class Workitemcomments
    {
        public string href { get; set; }
    }

    public class Html
    {
        public string href { get; set; }
    }

    public class Workitemtype
    {
        public string href { get; set; }
    }

    public class Fields1
    {
        public string href { get; set; }
    }

    public class Relation
    {
        public string rel { get; set; }
        public string url { get; set; }
        public Attributes attributes { get; set; }
    }

    public class Attributes
    {
        public bool isLocked { get; set; }
        public string name { get; set; }
    }


}

