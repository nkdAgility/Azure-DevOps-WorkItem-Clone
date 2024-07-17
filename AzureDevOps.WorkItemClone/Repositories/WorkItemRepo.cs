using AzureDevOps.WorkItemClone.DataContracts;
using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDevOps.WorkItemClone.Repositories
{
    public interface IWorkItemRepository
    {
        CashedWorkItems Data {get;}
        IAsyncEnumerable<(int total, int processed, string loadingFrom)> GetWorkItemsFullAsync();
    }
    public interface IPersistantCache
    {
        Task SaveToCache();
        Task LoadFromCache();
    }

    public class WorkItemRepository : IWorkItemRepository
    {
        public string OrganisationName { get; private set; }
        public string ProjectName { get; private set; }
        private string AccesToken {  get;  set; }
        public int ParentId { get; private set; }

        private AzureDevOpsApi _context;
        private string cacheWorkItemsFile;
        public CashedWorkItems Data { get { return cachedWorkItems; } }

        CashedWorkItems cachedWorkItems = null;

        public WorkItemRepository(string cachePath, string organisationName, string projectName, string accessToken, int parentId)
        {
            if (string.IsNullOrEmpty(organisationName))
            {
                throw new ArgumentNullException(nameof(organisationName));
            }
            this.OrganisationName = organisationName;
            if (string.IsNullOrEmpty(projectName))
            {
                throw new ArgumentNullException(nameof(projectName));
            }
            this.ProjectName = projectName;
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentNullException(nameof(accessToken));
            }
            this.AccesToken = accessToken;
            if (parentId == 0)
            {
                throw new ArgumentNullException(nameof(parentId));
            }
            this.ParentId = parentId;
            _context = new AzureDevOpsApi(accessToken, organisationName, projectName);
            cacheWorkItemsFile = $"{cachePath}\\cache-{organisationName}-{projectName}-{ParentId}.json";
        }


        public async IAsyncEnumerable<(int total, int processed, string loadingFrom)> GetWorkItemsFullAsync()
        {
            if (System.IO.File.Exists(cacheWorkItemsFile))
            {
                // load Cache
                try
                {
                    cachedWorkItems = JsonConvert.DeserializeObject<CashedWorkItems>(System.IO.File.ReadAllText(cacheWorkItemsFile));
                }
                catch (Exception ex)
                {
                    // failed to load:: do nothing we will refresh the cache.
                }
                if (cachedWorkItems != null)
                {
                    //Test Cache date
                    QueryResults? changedWorkItems = await _context.GetWiqlQueryResults("Select [System.Id] From WorkItems Where [System.TeamProject] = '@project' AND [System.Parent] = @id AND [System.ChangedDate] > '@changeddate' order by [System.CreatedDate] desc", new Dictionary<string, string>() { { "@id", ParentId.ToString() }, { "@changeddate", cachedWorkItems.queryDatetime.AddDays(-1).ToString("yyyy-MM-dd") } });
                    if (changedWorkItems?.workItems.Length == 0)
                    {
                        yield return (cachedWorkItems.workitems.Count(), cachedWorkItems.workitems.Count(), "cache");
                    }
                    else
                    {
                        cachedWorkItems = null;
                    }
                }
            }
            if (cachedWorkItems == null)
            {
                
                QueryResults? templateWorkItemLight;
                templateWorkItemLight = await _context.GetWiqlQueryResults("Select [System.Id] From WorkItems Where [System.TeamProject] = '@project' AND [System.Parent] = @id order by [System.CreatedDate] desc", new Dictionary<string, string>() { { "@id", ParentId.ToString() } });
                cachedWorkItems = new CashedWorkItems() { queryDatetime = templateWorkItemLight.asOf, workitems = new List<WorkItemFull>() };
                int count = 1;
                foreach (var item in templateWorkItemLight?.workItems)
                {
                    WorkItemFull result = await _context.GetWorkItem((int)item.id);
                    if (result != null)
                    {
                        cachedWorkItems.workitems.Add(result);
                    }
                    yield return (templateWorkItemLight.workItems.Count(), count, "server");
                    count++;
                }
                System.IO.File.WriteAllText(cacheWorkItemsFile, JsonConvert.SerializeObject(cachedWorkItems, Formatting.Indented));
            }
                
        }

    }
}
