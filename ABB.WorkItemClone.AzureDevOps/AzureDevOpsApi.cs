using ABB.WorkItemClone.AzureDevOps.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ABB.WorkItemClone.AzureDevOps
{
    public class AzureDevOpsApi
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

        private async Task<string> GetResult(string apiToCall, string? post)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "ManagedClientConsoleAppSample");
                client.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");
                client.DefaultRequestHeaders.Add("Authorization", _authHeader);

                HttpResponseMessage response;
                if (string.IsNullOrEmpty(post))
                    {
                    response = await client.GetAsync(apiToCall);
                } else
                {
                    response = await client.PostAsync(apiToCall, new StringContent(post, System.Text.Encoding.UTF8, "application/json"));
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


        private async Task<T?> GetObjectResult<T>(string apiCallUrl, string? post = null)
        {
            string? result = "";
            try
            {
                result = await GetResult(apiCallUrl, post);
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
                Console.WriteLine($"Result: {result}");
                Console.WriteLine($"ObjectType: {typeof(T).ToString}");
                Console.WriteLine($"-----------------------------");
                Console.WriteLine(ex.ToString());
                Console.WriteLine($"-----------------------------");
            }
            return default(T);
        }

     
    }
}
