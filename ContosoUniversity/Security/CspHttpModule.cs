using System;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Adds Content-Security-Policy and related security headers to every response (SOC2 / XSS defense-in-depth).
    /// The policy can be overridden with the CSP_POLICY environment variable; GCP Cloud Armor provides the WAF layer.
    /// </summary>
    public class CspHttpModule : IHttpModule
    {
        public const string DefaultPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
            "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

        public static string GetPolicy()
        {
            var policy = Environment.GetEnvironmentVariable("CSP_POLICY");
            return string.IsNullOrWhiteSpace(policy) ? DefaultPolicy : policy;
        }

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += (sender, e) =>
            {
                var response = ((HttpApplication)sender).Context.Response;
                response.Headers.Set("Content-Security-Policy", GetPolicy());
                response.Headers.Set("X-Content-Type-Options", "nosniff");
                response.Headers.Set("X-Frame-Options", "DENY");
                response.Headers.Set("Referrer-Policy", "strict-origin-when-cross-origin");
            };
        }

        public void Dispose()
        {
        }
    }
}
