using System;
using System.Collections.Generic;
using System.Linq;
using Ganss.Xss;
using Google.Cloud.SecretManager.V1;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Sanitization policy (allowed tags / attributes / URI schemes) stored as a versioned secret in
    /// GCP Secret Manager. JSON shape: {"allowedTags":["b","i"],"allowedAttributes":["class"],"allowedSchemes":["https"]}
    /// </summary>
    public class HtmlSanitizerPolicy
    {
        [JsonProperty("allowedTags")]
        public List<string> AllowedTags { get; set; } = new List<string>();

        [JsonProperty("allowedAttributes")]
        public List<string> AllowedAttributes { get; set; } = new List<string>();

        [JsonProperty("allowedSchemes")]
        public List<string> AllowedSchemes { get; set; } = new List<string>();
    }

    /// <summary>
    /// Central Ganss.Xss HtmlSanitizer used instead of Html.Raw / HtmlString. The policy is loaded from
    /// GCP Secret Manager (12-factor configuration through the environment):
    ///   GCP_PROJECT_ID (or GOOGLE_CLOUD_PROJECT)  - project that owns the secret
    ///   HTML_SANITIZER_POLICY_SECRET              - secret name (default "html-sanitizer-policy")
    ///   HTML_SANITIZER_POLICY_VERSION             - secret version (default "latest")
    ///   HTML_SANITIZER_POLICY_JSON                - optional local override (e.g. for development)
    /// If the policy cannot be loaded, the strictest policy is used (no tags, no attributes), so content
    /// is always sanitized (fail-closed).
    /// </summary>
    public static class HtmlSanitizerService
    {
        private static readonly object SyncRoot = new object();
        private static HtmlSanitizer sanitizer;
        private static DateTime loadedAtUtc = DateTime.MinValue;
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);

        public static string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }
            return GetSanitizer().Sanitize(html);
        }

        /// <summary>Sanitizes the value and returns the original object unchanged when it is not a string.</summary>
        public static object Sanitize(object value)
        {
            var text = value as string;
            return text == null ? value : Sanitize(text);
        }

        private static HtmlSanitizer GetSanitizer()
        {
            var current = sanitizer;
            if (current != null && DateTime.UtcNow - loadedAtUtc < RefreshInterval)
            {
                return current;
            }

            lock (SyncRoot)
            {
                if (sanitizer == null || DateTime.UtcNow - loadedAtUtc >= RefreshInterval)
                {
                    sanitizer = BuildSanitizer(LoadPolicy());
                    loadedAtUtc = DateTime.UtcNow;
                }
                return sanitizer;
            }
        }

        private static HtmlSanitizerPolicy LoadPolicy()
        {
            try
            {
                var json = Environment.GetEnvironmentVariable("HTML_SANITIZER_POLICY_JSON");
                if (string.IsNullOrWhiteSpace(json))
                {
                    var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
                                    ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
                    if (string.IsNullOrWhiteSpace(projectId))
                    {
                        return new HtmlSanitizerPolicy();
                    }

                    var secretName = Environment.GetEnvironmentVariable("HTML_SANITIZER_POLICY_SECRET");
                    if (string.IsNullOrWhiteSpace(secretName))
                    {
                        secretName = "html-sanitizer-policy";
                    }
                    var version = Environment.GetEnvironmentVariable("HTML_SANITIZER_POLICY_VERSION");
                    if (string.IsNullOrWhiteSpace(version))
                    {
                        version = "latest";
                    }

                    var client = SecretManagerServiceClient.Create();
                    var response = client.AccessSecretVersion(new SecretVersionName(projectId, secretName, version));
                    json = response.Payload.Data.ToStringUtf8();
                }

                return JsonConvert.DeserializeObject<HtmlSanitizerPolicy>(json) ?? new HtmlSanitizerPolicy();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("{\"severity\":\"WARNING\",\"message\":\"Unable to load HTML sanitizer policy from GCP Secret Manager; using strictest policy\",\"error\":\""
                                        + (ex.Message ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "'").Replace("\r", " ").Replace("\n", " ") + "\"}");
                return new HtmlSanitizerPolicy();
            }
        }

        private static HtmlSanitizer BuildSanitizer(HtmlSanitizerPolicy policy)
        {
            var result = new HtmlSanitizer();
            result.AllowedTags.Clear();
            result.AllowedAttributes.Clear();
            result.AllowedSchemes.Clear();

            foreach (var tag in (policy.AllowedTags ?? new List<string>()).Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                result.AllowedTags.Add(tag.Trim().ToLowerInvariant());
            }
            foreach (var attribute in (policy.AllowedAttributes ?? new List<string>()).Where(a => !string.IsNullOrWhiteSpace(a)))
            {
                result.AllowedAttributes.Add(attribute.Trim().ToLowerInvariant());
            }
            foreach (var scheme in (policy.AllowedSchemes ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                result.AllowedSchemes.Add(scheme.Trim().ToLowerInvariant());
            }
            return result;
        }
    }
}
