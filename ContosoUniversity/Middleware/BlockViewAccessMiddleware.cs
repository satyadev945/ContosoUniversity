using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// ASP.NET Core middleware replacing the IIS "BlockViewHandler"
    /// (System.Web.HttpNotFoundHandler mapped in Views/Web.config).
    /// Direct requests for anything under /Views return 404.
    /// </summary>
    public class BlockViewAccessMiddleware
    {
        private readonly RequestDelegate _next;

        public BlockViewAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path;
            if (path.StartsWithSegments("/Views", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            return _next(context);
        }
    }

    public static class BlockViewAccessMiddlewareExtensions
    {
        public static IApplicationBuilder UseBlockViewAccess(this IApplicationBuilder app)
        {
            return app.UseMiddleware<BlockViewAccessMiddleware>();
        }
    }
}
