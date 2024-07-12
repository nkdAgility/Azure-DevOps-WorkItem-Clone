using Elmah.Io.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDevOps.WorkItemClone
{
    public static class Telemetry
    {
        private static IElmahioAPI elmahIoClient;
        public static bool Enabled { get; set; }

        public static void Initialize(string? version)
        {
            if (elmahIoClient != null)
            {
                return;
            }

            elmahIoClient = ElmahioAPI.Create("7589821e832a4ae1a1170f8201def634", new ElmahIoOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                UserAgent = "Azure-DevOps-Work-Item-Clone",
            });
            elmahIoClient.Messages.OnMessage += (sender, args) => args.Message.Version = version;

            Enabled = true;
        }

        public static void TrackException(Exception ex, string category = "none", Dictionary<string, string>? properties = null)
        {
            if (Enabled && ex != null && elmahIoClient != null)
            {
                var baseException = ex.GetBaseException();
                var createMessage = new CreateMessage
                {
                    DateTime = DateTime.UtcNow,
                    Detail = ex.ToString(),
                    Type = baseException.GetType().FullName,
                    Title = baseException.Message ?? "An error occurred",
                    Data = properties?.Select(p => new Item(p.Key, p.Value)).ToList(),
                    Severity = "Error",
                    Source = baseException.Source,
                    User = Environment.UserName,
                    Hostname = Hostname(),
                    Application = "Azure-DevOps-Work-Item-Clone",
                    ServerVariables = new List<Item>
                    {
                        new Item("User-Agent", $"X-ELMAHIO-APPLICATION; OS={Environment.OSVersion.Platform}; OSVERSION={Environment.OSVersion.Version}; ENGINE=Azure-DevOps-Work-Item-Clone"),
                    }
                };

                elmahIoClient.Messages.CreateAndNotify(new Guid("99d8f645-5775-4ace-9aa5-f3fd20b850b9"), createMessage);
            }
        }


        private static string Hostname()
        {
            var machineName = Environment.MachineName;
            if (!string.IsNullOrWhiteSpace(machineName)) return machineName;

            return Environment.GetEnvironmentVariable("COMPUTERNAME");
        }
    }
}

