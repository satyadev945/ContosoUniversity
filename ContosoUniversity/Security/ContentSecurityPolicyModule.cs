using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that emits Content-Security-Policy (and related hardening) headers on every response.
    /// The policy can be overridden per environment through the CSP_POLICY environment variable or the
    /// "Csp:Policy" appSetting (e.g. injected by AWS Elastic Beanstalk / ECS / Parameter Store).
    /// Set "Csp:ReportOnly" (or CSP_REPORT_ONLY) to "true" to roll out in report-only mode.
    /// Complements AWS WAF managed rules (AWSManagedRulesCommonRuleSet / KnownBadInputs) at the edge.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        internal const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'self'";

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var response = app.Response;
            if (response == null || response.HeadersWritten)
            {
                return;
            }

            var policy = GetSetting("CSP_POLICY", "Csp:Policy");
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = DefaultPolicy;
            }

            var reportOnlyValue = GetSetting("CSP_REPORT_ONLY", "Csp:ReportOnly");
            var reportOnly = string.Equals(reportOnlyValue, "true", StringComparison.OrdinalIgnoreCase);
            var headerName = reportOnly ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy";

            response.Headers.Remove(headerName);
            response.Headers.Add(headerName, policy);

            response.Headers.Remove("X-Content-Type-Options");
            response.Headers.Add("X-Content-Type-Options", "nosniff");
        }

        private static string GetSetting(string envName, string appSettingName)
        {
            var value = Environment.GetEnvironmentVariable(envName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            return ConfigurationManager.AppSettings[appSettingName];
        }

        public void Dispose()
        {
        }
    }
}
