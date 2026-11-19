using System;
using System.Collections.Generic;
using System.Linq;
using Ganss.Xss;
using Google.Cloud.SecretManager.V1;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>Allow-list policy for HTML sanitization (stored as JSON in GCP Secret Manager).</summary>
    public class HtmlSanitizationPolicy
    {
        public List<string> AllowedTags { get; set; }
        public List<string> AllowedAttributes { get; set; }
        public List<string> AllowedSchemes { get; set; }
    }

    /// <summary>
    /// Central replacement for @Html.Raw()/HtmlString: every value that may reach a view as markup
    /// must pass through <see cref="Sanitize"/> (Ganss.Xss HtmlSanitizer).
    /// The policy (allowed tags / attributes / URL schemes) is a versioned secret in GCP Secret Manager:
    ///   HTML_SANITIZER_SECRET_ID       secret name (default "html-sanitizer-policy")
    ///   HTML_SANITIZER_SECRET_VERSION  version (default "latest")
    ///   GCP_PROJECT_ID / GOOGLE_CLOUD_PROJECT  project that owns the secret
    /// If the secret is unavailable a strict default (no tags, no attributes) is used, i.e. fail closed.
    /// </summary>
    public static class HtmlSanitizerService
    {
        private static readonly Lazy<HtmlSanitizer> Sanitizer = new Lazy<HtmlSanitizer>(Build);

        private static HtmlSanitizer Build()
        {
            var sanitizer = new HtmlSanitizer();
            var policy = LoadPolicy();

            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();

            if (policy != null)
            {
                foreach (var t in policy.AllowedTags ?? new List<string>()) sanitizer.AllowedTags.Add(t);
                foreach (var a in policy.AllowedAttributes ?? new List<string>()) sanitizer.AllowedAttributes.Add(a);
                foreach (var s in policy.AllowedSchemes ?? new List<string>()) sanitizer.AllowedSchemes.Add(s);
            }
            else
            {
                // Fail-closed default when the policy secret cannot be read
                foreach (var t in new[] { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li" }) sanitizer.AllowedTags.Add(t);
                sanitizer.AllowedSchemes.Add("https");
            }
            return sanitizer;
        }

        private static HtmlSanitizationPolicy LoadPolicy()
        {
            try
            {
                var project = Environment.GetEnvironmentVariable("GCP_PROJECT_ID")
                              ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
                if (string.IsNullOrEmpty(project))
                {
                    return null;
                }
                var secretId = Environment.GetEnvironmentVariable("HTML_SANITIZER_SECRET_ID") ?? "html-sanitizer-policy";
                var version = Environment.GetEnvironmentVariable("HTML_SANITIZER_SECRET_VERSION") ?? "latest";

                var client = SecretManagerServiceClient.Create();
                var name = new SecretVersionName(project, secretId, version);
                var response = client.AccessSecretVersion(name);
                return JsonConvert.DeserializeObject<HtmlSanitizationPolicy>(response.Payload.Data.ToStringUtf8());
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Returns the input sanitized against the centrally managed policy (null-safe).</summary>
        public static string Sanitize(string html)
        {
            return string.IsNullOrEmpty(html) ? html : Sanitizer.Value.Sanitize(html);
        }
    }
}
