using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// Replaces the IIS "BlockViewHandler" (System.Web.HttpNotFoundHandler registered in Views/Web.config).
    /// Prevents direct HTTP access to raw view files under /Views by returning 404.
    /// </summary>
    public class BlockViewMiddleware
    {
        private readonly RequestDelegate _next;

        public BlockViewMiddleware(RequestDelegate next)
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
}
