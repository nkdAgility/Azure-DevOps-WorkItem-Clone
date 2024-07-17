using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDevOps.WorkItemClone.DataContracts
{

    public class Query
    {
        public string id { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public QueryCreatedby createdBy { get; set; }
        public DateTime createdDate { get; set; }
        public QueryLastmodifiedby lastModifiedBy { get; set; }
        public DateTime lastModifiedDate { get; set; }
        public bool isFolder { get; set; }
        public bool hasChildren { get; set; }
        public bool isPublic { get; set; }
        public QueryLinks2 _links { get; set; }
        public string url { get; set; }
    }

    public class QueryCreatedby
    {
        public string displayName { get; set; }
        public string url { get; set; }
        public QueryLinks _links { get; set; }
        public string id { get; set; }
        public string uniqueName { get; set; }
        public string imageUrl { get; set; }
        public string descriptor { get; set; }
    }

    public class QueryLinks
    {
        public Avatar avatar { get; set; }
    }

    public class QueryAvatar
    {
        public string href { get; set; }
    }

    public class QueryLastmodifiedby
    {
        public string displayName { get; set; }
        public string url { get; set; }
        public QueryLinks1 _links { get; set; }
        public string id { get; set; }
        public string uniqueName { get; set; }
        public string imageUrl { get; set; }
        public string descriptor { get; set; }
    }

    public class QueryLinks1
    {
        public QueryAvatar1 avatar { get; set; }
    }

    public class QueryAvatar1
    {
        public string href { get; set; }
    }

    public class QueryLinks2
    {
        public QuerySelf self { get; set; }
        public QueryHtml html { get; set; }
        public QueryParent parent { get; set; }
    }

    public class QuerySelf
    {
        public string href { get; set; }
    }

    public class QueryHtml
    {
        public string href { get; set; }
    }

    public class QueryParent
    {
        public string href { get; set; }
    }

}
