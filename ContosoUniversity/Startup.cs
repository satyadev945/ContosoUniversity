using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    /// <summary>
    /// ASP.NET Core startup replacing the IIS-hosted Global.asax / MvcApplication (System.Web.HttpApplication)
    /// and the IIS handler/module configuration (system.webServer) in Web.config files.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Replaces FilterConfig.RegisterGlobalFilters (HandleErrorAttribute) with the ASP.NET Core exception handler.
            services.AddControllersWithViews();
            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Replaces Global.asax Application_Start. Only stateless, per-process registration happens here
            // (EF interceptors must be registered in every instance). Configuration/secrets are read from
            // GCP Secret Manager (see Security/HtmlSanitizerService.cs) and one-time initialization
            // (DB migrations/seeding) must run in a Cloud Run init container / job, never in app startup,
            // so scaled instances stay consistent.
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // CSP + security headers (equivalent of CspHttpModule registered in Web.config); complements GCP Cloud Armor WAF.
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["Content-Security-Policy"] = ContosoUniversity.Security.CspHttpModule.GetPolicy();
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    context.Response.Headers["X-Frame-Options"] = "DENY";
                    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    return Task.CompletedTask;
                });
                await next();
            });

            // Replaces Views/Web.config <system.webServer><handlers> BlockViewHandler:
            // direct requests into the Views folder always return 404.
            app.UseMiddleware<BlockViewMiddleware>();

            // Replaces BundleConfig / System.Web.Optimization: static files served by middleware.
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // Liveness/readiness probe endpoint for GKE.
                endpoints.MapHealthChecks("/healthz");

                // Replaces RouteConfig.RegisterRoutes
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }

    /// <summary>
    /// ASP.NET Core middleware equivalent of the IIS BlockViewHandler (System.Web.HttpNotFoundHandler).
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
            if (context.Request.Path.StartsWithSegments("/Views", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }
            return _next(context);
        }
    }
}
