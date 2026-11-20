using System;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// ASP.NET HttpModule that adds Content-Security-Policy (and companion security) headers
    /// to every response. Registered in Web.config (system.web/httpModules and
    /// system.webServer/modules). The policy can be overridden per environment with the
    /// CONTENT_SECURITY_POLICY environment variable (or the "ContentSecurityPolicy" appSetting).
    /// Complements AWS WAF managed rules (e.g. AWSManagedRulesCommonRuleSet) on the ALB/CloudFront.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        internal const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'none'";

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var response = ((HttpApplication)sender).Context.Response;
            var policy = Environment.GetEnvironmentVariable("CONTENT_SECURITY_POLICY");
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = System.Configuration.ConfigurationManager.AppSettings["ContentSecurityPolicy"];
            }
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = DefaultPolicy;
            }

            response.Headers.Remove("Content-Security-Policy");
            response.Headers.Add("Content-Security-Policy", policy);
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["X-Frame-Options"] = "DENY";
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }

        public void Dispose()
        {
        }
    }
}
