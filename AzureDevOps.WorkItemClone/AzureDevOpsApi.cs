using AzureDevOps.WorkItemClone.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AzureDevOps.WorkItemClone
{
    public class AzureDevOpsApi : IAsyncDisposable
    {
        private readonly string _authHeader;
        private readonly string _account;
        private readonly string _project;

        public string Project => _project;


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

        public async Task<QueryResults?> GetWiqlQueryResults(string wiqlQuery, Dictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                parameters = new Dictionary<string, string>();
            }
            if (!parameters.ContainsKey("@project"))
            {
                parameters.Add("@project", _project);
            }
            if (string.IsNullOrEmpty(wiqlQuery))
            {
                wiqlQuery = "Select [System.Id], [System.Title], [System.State] From WorkItems Where [System.TeamProject] = '@project' order by [System.CreatedDate] desc";
            }
            foreach (var param in parameters)
            {
                wiqlQuery = wiqlQuery.Replace(param.Key, param.Value);
            }
            string post = JsonConvert.SerializeObject(new
            {
                query = wiqlQuery
            });
            string apiCallUrl = $"https://dev.azure.com/{_account}/_apis/wit/wiql?api-version=7.2-preview.2";
            var result = await GetObjectResult<QueryResults>(apiCallUrl, post);
            return result.result;
        }

        public async Task<QueryResults?> GetWiqlQueryResults()
        {
            string post = JsonConvert.SerializeObject(new {
                query = $"Select [System.Id], [System.Title], [System.State] From WorkItems Where [System.TeamProject] = '{_project}' order by [System.CreatedDate] desc"
            });
            string apiCallUrl = $"https://dev.azure.com/{_account}/_apis/wit/wiql?api-version=7.2-preview.2";
            var result = await GetObjectResult<QueryResults>(apiCallUrl, post);
            return result.result;
        }

        public async Task<NodeClassification> GetNodeClassification(string nodePath)
        {
            //GET https://dev.azure.com/{organization}/{project}/_apis/wit/classificationnodes/{structureGroup}/{path}?api-version=7.2-preview.2
            string apiCallUrl = $"https://dev.azure.com/{_account}/{_project}/_apis/wit/classificationnodes/areas/{nodePath.Replace(@"\", "/")}?api-version=7.2-preview.2";
            var result = await GetObjectResult<NodeClassification>(apiCallUrl);
            if (result.statusCode == HttpStatusCode.NotFound)
            {
                return null;
            } else
            {
                return result.result;
            }     
        }

    public async Task<FieldItem> GetFieldOnWorkItem(string wit, string fieldRefName)
        {
            //GET https://dev.azure.com/{organization}/{project}/_apis/wit/workitemtypes/{type}/fields/{field}?api-version=7.2-preview.3
            string apiCallUrl = $"https://dev.azure.com/{_account}/{_project}/_apis/wit/workitemtypes/{wit}{fieldRefName}?api-version=7.1-preview.3";
            var result = await GetObjectResult<FieldItem>(apiCallUrl);
            return result.result;
        }
        public async Task<WorkItemFull?> GetWorkItem(int id)
        {
            string apiCallUrl = $"https://dev.azure.com/{_account}/{_project}/_apis/wit/workitems/{id}?$expand=All&api-version=7.2-preview.3";
            var result = await GetObjectResult<WorkItemFull>(apiCallUrl);
            return result.result;
        }

        public async Task<WorkItemFull?> CreateWorkItem(WorkItemAdd itemAdd, string workItemType)
        {
            // POST https://dev.azure.com/fabrikam/{project}/_apis/wit/workitems/${type}?api-version=7.1-preview.3
            string post = JsonConvert.SerializeObject(itemAdd.Operations);
            string apiCallUrl = $"https://dev.azure.com/{_account}/{_project}/_apis/wit/workitems/${workItemType}?api-version=7.1-preview.3";
            var result = await GetObjectResult<WorkItemFull>(apiCallUrl, post, "application/json-patch+json");
            return result.result;

        }

        private async Task<(string content, HttpStatusCode statusCode,string reasonPhrase)> GetResult(string apiToCall, string? post, string? mediaType = "application/json")
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
                    return (await response.Content.ReadAsStringAsync(), response.StatusCode, response.ReasonPhrase);
                } else
                {
                    return (string.Empty, response.StatusCode, response.ReasonPhrase);
                }
            }
        }


        private async Task<(T? result, HttpStatusCode? statusCode)> GetObjectResult<T>(string apiCallUrl, string? post = null, string? mediaType = null)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("apiCallUrl", apiCallUrl);
            properties.Add("mediaType", mediaType);
            properties.Add("post", post);
            properties.Add("ObjectType", typeof(T).Name);
            (string? content, HttpStatusCode? statusCode, string? reasonPhrase) result = (null, null, null);
            try
            {
                result = await GetResult(apiCallUrl, post, mediaType);
                properties.Add("result", result.content);
                properties.Add("StatusCode", result.statusCode?.ToString());
                properties.Add("ReasonPhrase", result.reasonPhrase);
                if (result.statusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(result.content))
                {
                    return (JsonConvert.DeserializeObject<T>(result.content), result.statusCode);
                } else
                {
                   switch (result.statusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            throw new Exception("Bad Request");
                        case HttpStatusCode.Forbidden:
                            throw new Exception("Forbidden");
                        case HttpStatusCode.InternalServerError:
                            throw new Exception("Internal Server Error");    
                    }
                    return (default(T), result.statusCode);
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex, result.statusCode?.ToString(), properties);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"-----------------------------");
                sb.AppendLine($"Azure DevOps API Call Failed!");
        
                sb.AppendLine($"Post: {post}");
                sb.AppendLine($"Result: {result}");
                sb.AppendLine($"ObjectType: {typeof(T).Name}");
                    sb.AppendLine($"-----------------------------");
                sb.AppendLine(ex.ToString());
                sb.AppendLine($"-----------------------------");
                // Should be logger
                
                if (!System.IO.Directory.Exists("./.errors"))
                    {
                    System.IO.Directory.CreateDirectory("./.errors");
                }
                string errorFile = $"./.errors/{DateTime.Today.ToString("yyyyyMMddHHmmss")}.txt";
                System.IO.File.WriteAllText(errorFile, sb.ToString());
                AnsiConsole.WriteLine($"Error logged to: {errorFile}");

            }
            return (default(T), null);
        }

        public ValueTask DisposeAsync()
        {
            return new(Task.Delay(TimeSpan.FromSeconds(1)));
        }
    }

}
