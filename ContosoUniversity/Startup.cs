using ContosoUniversity.DAL;
using ContosoUniversity.Initialization;
using ContosoUniversity.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    /// <summary>
    /// Replaces Global.asax.cs (System.Web.HttpApplication / IIS pipeline) with the
    /// ASP.NET Core middleware pipeline hosted on Kestrel.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Former Application_Start: now coordinated across all scaled instances using
            // AWS Systems Manager Parameter Store (shared config) and a DynamoDB lock
            // (cluster-wide one-time work). EF6 interceptors are per-process state.
            DistributedStartupInitializer.InitializeAsync(
                clusterWideInitialization: null,
                perInstanceInitialization: () =>
                {
                    DbInterception.Add(new SchoolInterceptorTransientErrors());
                    DbInterception.Add(new SchoolInterceptorLogging());
                }).GetAwaiter().GetResult();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy headers (pairs with AWS WAF managed rules)
            app.UseMiddleware<ContentSecurityPolicyMiddleware>();

            // Former Views/Web.config BlockViewHandler
            app.UseMiddleware<BlockViewMiddleware>();

            // Replaces BundleConfig / bundled static content
            app.UseStaticFiles();

            app.UseRouting();

            // Former RouteConfig.RegisterRoutes
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
