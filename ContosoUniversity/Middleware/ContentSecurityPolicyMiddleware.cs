using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// Kestrel/ASP.NET Core equivalent of ContentSecurityPolicyModule: adds the
    /// Content-Security-Policy header to every response. Override via the
    /// CONTENT_SECURITY_POLICY environment variable. Complements AWS WAF managed rules.
    /// </summary>
    public class ContentSecurityPolicyMiddleware
    {
        private const string DefaultPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; " +
            "base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

        private readonly RequestDelegate _next;
        private readonly string _policy;

        public ContentSecurityPolicyMiddleware(RequestDelegate next)
        {
            _next = next;
            var policy = Environment.GetEnvironmentVariable("CONTENT_SECURITY_POLICY");
            _policy = string.IsNullOrWhiteSpace(policy) ? DefaultPolicy : policy;
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
}
