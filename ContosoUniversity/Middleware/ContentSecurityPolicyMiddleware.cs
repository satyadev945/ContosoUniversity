using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Middleware
{
    // Kestrel/ASP.NET Core replacement for a System.Web IHttpModule: adds Content-Security-Policy
    // (and companion hardening headers) to every response. Defense-in-depth with a GCP Cloud Armor
    // WAF security policy (XSS preconfigured rules) attached to the external HTTP(S) load balancer.
    // The policy can be overridden without redeploying through the CONTENT_SECURITY_POLICY env var
    // (or the "ContentSecurityPolicy" configuration/appSettings key).
    public class ContentSecurityPolicyMiddleware
    {
        public const string DefaultPolicy =
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

        private readonly RequestDelegate _next;
        private readonly string _policy;

        public ContentSecurityPolicyMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _policy = Environment.GetEnvironmentVariable("CONTENT_SECURITY_POLICY")
                      ?? configuration["ContentSecurityPolicy"]
                      ?? DefaultPolicy;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["Content-Security-Policy"] = _policy;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                return Task.CompletedTask;
            });
            return _next(context);
        }
    }

    public static class ContentSecurityPolicyExtensions
    {
        public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ContentSecurityPolicyMiddleware>();
        }
    }
}
