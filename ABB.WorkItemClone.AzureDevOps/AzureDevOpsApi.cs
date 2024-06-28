using ABB.WorkItemClone.AzureDevOps.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ABB.WorkItemClone.AzureDevOps
{
    public class AzureDevOpsApi : IAsyncDisposable
    {
        private readonly string _authHeader;
        private readonly string _account;
        private readonly string _project;


        public AzureDevOpsApi(string token, string account, string project)
        {
            var authTool = new Authenticator();
            _authHeader = authTool.AuthenticationCommand(token).Result;
            _account = account;
            _project = project;
        }

        public Task<List<WorkItemFull>> GetWorkItemsFullAsync()
        {
            var fakeItems = GetWiqlQueryResults().Result;

            List<WorkItemFull> realItems = new List<WorkItemFull>();
            foreach (var item in fakeItems.workItems)
            {
                realItems.Add(GetWorkItem((int)item.id).Result);
            }
            return Task.FromResult(realItems);
        }

        public async IAsyncEnumerable<WorkItemFull> GetWorkItemsFullAsync(Workitem[] itemsToGet)
        {
            for (var i = 0; i < itemsToGet.Length; ++i)
            {
                //await Task.Delay(TimeSpan.FromMilliseconds(1000));
                WorkItemFull result = await GetWorkItem((int)itemsToGet[i].id);
                //WorkItemFull result = new WorkItemFull();
                yield return result;
            }
        }


        public async Task<QueryResults?> GetWiqlQueryResults()
        {
            string post = JsonConvert.SerializeObject(new {
                query = $"Select [System.Id], [System.Title], [System.State] From WorkItems Where [System.TeamProject] = '{_project}' order by [System.CreatedDate] desc"
            });
            string apiCallUrl = $"https://dev.azure.com/{_account}/_apis/wit/wiql?api-version=7.2-preview.2";
            return await GetObjectResult<QueryResults>(apiCallUrl, post);
        }

        public async Task<WorkItemFull?> GetWorkItem(int id)
        {
            string apiCallUrl = $"https://dev.azure.com/{_account}/{_project}/_apis/wit/workitems/{id}?$expand=All&api-version=7.2-preview.3";
            return await GetObjectResult<WorkItemFull>(apiCallUrl);
        }

        public async Task<WorkItemFull?> CreateWorkItem(WorkItemAdd itemAdd, string workItemType)
        {
            // POST https://dev.azure.com/fabrikam/{project}/_apis/wit/workitems/${type}?api-version=7.1-preview.3
            string post = JsonConvert.SerializeObject(itemAdd.Operations);
            string apiCallUrl = $"https://dev.azure.com/{_account}/{_project}/_apis/wit/workitems/${workItemType}?api-version=7.1-preview.3";
            return await GetObjectResult<WorkItemFull>(apiCallUrl, post, "application/json-patch+json");

        }

        private async Task<string> GetResult(string apiToCall, string? post, string? mediaType = "application/json")
        {
            if (string.IsNullOrEmpty(mediaType))
            {
                mediaType = "application/json";
            }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
                client.DefaultRequestHeaders.Add("User-Agent", "ManagedClientConsoleAppSample");
                client.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");
                client.DefaultRequestHeaders.Add("Authorization", _authHeader);

                HttpResponseMessage response;
                if (string.IsNullOrEmpty(post))
                    {
                    response = await client.GetAsync(apiToCall);
                } else
                {
                    response = await client.PostAsync(apiToCall, new StringContent(post, System.Text.Encoding.UTF8, mediaType));
                }                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException();
                }
                else
                {
                    throw new Exception($"Result::{response.StatusCode}:{response.ReasonPhrase}");
                }
            }
            return string.Empty;
        }


        private async Task<T?> GetObjectResult<T>(string apiCallUrl, string? post = null, string? mediaType = null)
        {
            string? result = "";
            try
            {
                result = await GetResult(apiCallUrl, post, mediaType);
                if (!string.IsNullOrEmpty(result))
                {
                    return JsonConvert.DeserializeObject<T>(result);
                }
            }
            catch (Exception ex)
            {
                // Should be logger
                Console.WriteLine($"-----------------------------");
                Console.WriteLine($"Azure DevOps API Call Failed!");
                Console.WriteLine($"apiCallUrl: {apiCallUrl}");
                Console.WriteLine($"mediaType: {mediaType}");
                Console.WriteLine($"Post: {post}");
                Console.WriteLine($"Result: {result}");
                Console.WriteLine($"ObjectType: {typeof(T).ToString}");
                Console.WriteLine($"-----------------------------");
                Console.WriteLine(ex.ToString());
                Console.WriteLine($"-----------------------------");
            }
            return default(T);
        }

        public ValueTask DisposeAsync()
        {
            return new(Task.Delay(TimeSpan.FromSeconds(1)));
        }
    }
}
