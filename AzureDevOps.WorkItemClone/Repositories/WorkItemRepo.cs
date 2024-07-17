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
        IAsyncEnumerable<(int total, int processed)> GetWorkItemsFullAsync();
        //Task<WorkItemFull> GetWorkItemByIdAsync(int id);
        //Task<IEnumerable<WorkItemFull>> GetAllWorkItemAsync();
        //Task AddWorkItemAsync(WorkItemFull wif);
        //Task UpdateWorkItemAsync(WorkItemFull wif);
        //Task DeleteWorkItemAsync(int id);
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
        public string ParentId { get; private set; }

        private AzureDevOpsApi _context;
        private string cacheWorkItemsFile;
        public CashedWorkItems WorkItems { get { return cachedWorkItems; } }

        CashedWorkItems cachedWorkItems = null;

        public WorkItemRepository(string cachePath, string organisationName, string projectName, string accessToken, string parentId)
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
            if (string.IsNullOrEmpty(ParentId))
            {
                throw new ArgumentNullException(nameof(parentId));
            }
            this.ParentId = parentId;
            _context = new AzureDevOpsApi(organisationName, projectName, accessToken);
            cacheWorkItemsFile = $"{cachePath}\\cache-{organisationName}-{projectName}-{ParentId}.json";
        }


        public async IAsyncEnumerable<(int total, int processed)> GetWorkItemsFullAsync()
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
                    QueryResults? changedWorkItems = await _context.GetWiqlQueryResults("Select [System.Id] From WorkItems Where [System.TeamProject] = '@project' AND [System.Parent] = @id AND [System.ChangedDate] > '@changeddate' order by [System.CreatedDate] desc", new Dictionary<string, string>() { { "@id", ParentId }, { "@changeddate", cachedWorkItems.queryDatetime.AddDays(-1).ToString("yyyy-MM-dd") } });
                    if (changedWorkItems?.workItems.Length == 0)
                    {
                        yield return (cachedWorkItems.workitems.Count(), cachedWorkItems.workitems.Count());
                    }
                    else
                    {
                        cachedWorkItems = null;
                    }
                }
            }
            if (cachedWorkItems == null)
            {
                cachedWorkItems = new CashedWorkItems() { queryDatetime = DateTime.Now, workitems = new List<WorkItemFull>() };
                QueryResults? templateWorkItemLight;
                templateWorkItemLight = await _context.GetWiqlQueryResults("Select [System.Id] From WorkItems Where [System.TeamProject] = '@project' AND [System.Parent] = @id order by [System.CreatedDate] desc", new Dictionary<string, string>() { { "@id", ParentId.ToString() } });
                int count = 1;
                foreach (var item in templateWorkItemLight?.workItems)
                {
                    WorkItemFull result = await _context.GetWorkItem((int)item.id);
                    if (result != null)
                    {
                        cachedWorkItems.workitems.Add(result);
                    }
                    yield return (cachedWorkItems.workitems.Count(), count);
                    count++;
                }
                System.IO.File.WriteAllText(cacheWorkItemsFile, JsonConvert.SerializeObject(cachedWorkItems, Formatting.Indented));
            }
                
        }

    }
}
