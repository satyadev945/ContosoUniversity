using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Centralised configuration from Google Cloud Secret Manager (replaces per-instance Global.asax
    /// Application_Start configuration). Every scaled instance reads the same secret values.
    ///
    /// SECRET_MANAGER_SECRETS = "ENV_NAME=secretId[:version],OTHER_ENV=otherSecret"
    /// GCP_PROJECT_ID (or GOOGLE_CLOUD_PROJECT) identifies the project.
    /// Authentication uses the workload's service account via the GCP metadata server.
    /// Values already present as environment variables are not overridden (so Cloud Run
    /// secret-as-env mounts continue to work). Does nothing when not configured.
    /// </summary>
    public static class GcpSecretManagerLoader
    {
        private const string MetadataTokenUrl =
            "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/token";

        public static void LoadIntoEnvironment()
        {
            var mapping = Environment.GetEnvironmentVariable("SECRET_MANAGER_SECRETS");
            if (string.IsNullOrWhiteSpace(mapping))
            {
                return;
            }
            var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
                            ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new InvalidOperationException("GCP_PROJECT_ID is required when SECRET_MANAGER_SECRETS is set.");
            }

            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
            {
                var token = GetAccessTokenAsync(http).GetAwaiter().GetResult();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                foreach (var pair in mapping.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = pair.Split(new[] { '=' }, 2);
                    if (parts.Length != 2) continue;
                    var envName = parts[0].Trim();
                    var secret = parts[1].Trim().Split(new[] { ':' }, 2);
                    var version = secret.Length > 1 ? secret[1] : "latest";

                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envName)))
                    {
                        continue;
                    }
                    var url = "https://secretmanager.googleapis.com/v1/projects/" + projectId +
                              "/secrets/" + secret[0] + "/versions/" + version + ":access";
                    var json = http.GetStringAsync(url).GetAwaiter().GetResult();
                    var data = (string)JObject.Parse(json)["payload"]["data"];
                    Environment.SetEnvironmentVariable(envName, Encoding.UTF8.GetString(Convert.FromBase64String(data)));
                }
            }
        }

        private static async Task<string> GetAccessTokenAsync(HttpClient http)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, MetadataTokenUrl);
            request.Headers.Add("Metadata-Flavor", "Google");
            var response = await http.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return (string)JObject.Parse(body)["access_token"];
        }
    }
}
