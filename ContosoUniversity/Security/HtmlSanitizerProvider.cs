using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.Xss;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Central Ganss.Xss HtmlSanitizer factory. The allowed tags / attributes / URI schemes are
    /// stored centrally in AWS Secrets Manager so they can be managed once for all tenants.
    ///
    /// Environment variables:
    ///   HTML_SANITIZER_SECRET_ID - name or ARN of the secret (JSON, see <see cref="SanitizerConfig"/>).
    ///   AWS_REGION               - region of the secret (falls back to the default AWS SDK region resolution).
    ///
    /// If the secret is not configured or cannot be read, the sanitizer falls back to the safe
    /// HtmlSanitizer defaults (no dangerous tags/attributes are ever allowed by default).
    /// </summary>
    public static class HtmlSanitizerProvider
    {
        private class SanitizerConfig
        {
            [JsonProperty("allowedTags")]
            public List<string> AllowedTags { get; set; }

            [JsonProperty("allowedAttributes")]
            public List<string> AllowedAttributes { get; set; }

            [JsonProperty("allowedSchemes")]
            public List<string> AllowedSchemes { get; set; }
        }

        private static readonly Lazy<HtmlSanitizer> LazySanitizer = new Lazy<HtmlSanitizer>(CreateSanitizer);

        /// <summary>Sanitizes untrusted HTML/text. Returns null for null input.</summary>
        public static string Sanitize(string html)
        {
            if (html == null)
            {
                return null;
            }

            // HtmlSanitizer instances are not guaranteed thread safe for mutation, but Sanitize is read-only
            // once configured.
            return LazySanitizer.Value.Sanitize(html);
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var sanitizer = new HtmlSanitizer();

            try
            {
                var config = Task.Run(LoadConfigAsync).GetAwaiter().GetResult();
                if (config != null)
                {
                    Apply(sanitizer.AllowedTags, config.AllowedTags);
                    Apply(sanitizer.AllowedAttributes, config.AllowedAttributes);
                    Apply(sanitizer.AllowedSchemes, config.AllowedSchemes);
                }
            }
            catch (Exception ex)
            {
                // Never fail open: keep the safe defaults when the secret can't be loaded.
                Console.Error.WriteLine("{\"level\":\"warn\",\"message\":\"HtmlSanitizer config could not be loaded from AWS Secrets Manager; using defaults\",\"error\":\"" +
                                        ex.GetType().Name + "\"}");
            }

            return sanitizer;
        }

        private static void Apply(ISet<string> target, List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }

            target.Clear();
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                {
                    target.Add(v.Trim());
                }
            }
        }

        private static async Task<SanitizerConfig> LoadConfigAsync()
        {
            var secretId = Environment.GetEnvironmentVariable("HTML_SANITIZER_SECRET_ID");
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return null;
            }

            var region = Environment.GetEnvironmentVariable("AWS_REGION");
            using (var client = string.IsNullOrWhiteSpace(region)
                ? new AmazonSecretsManagerClient()
                : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region)))
            {
                var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId })
                    .ConfigureAwait(false);
                return JsonConvert.DeserializeObject<SanitizerConfig>(response.SecretString);
            }
        }
    }
}
