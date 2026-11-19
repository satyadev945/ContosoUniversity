using ContosoUniversity.DAL;
using ContosoUniversity.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    // Replaces Global.asax / System.Web.HttpApplication (IIS-specific).
    // One-time initialization (migrations) runs in an init step (INIT_ONLY=true, see AppInitializer);
    // configuration/secrets come from GCP Secret Manager (see GcpSecretManagerLoader).
    // Only stateless, process-local registration remains here.
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Replaces AreaRegistration/FilterConfig/BundleConfig: MVC services and the global exception handling
            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Replaces HandleErrorAttribute / IIS custom errors
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy headers (replaces web.config customHeaders / System.Web HttpModule);
            // paired with a GCP Cloud Armor WAF policy at the load balancer for defense-in-depth.
            app.UseContentSecurityPolicy();

            // Replaces the IIS <system.webServer><handlers> BlockViewHandler from Views/Web.config:
            // never serve raw view files directly.
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/Views", System.StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 404;
                    return;
                }
                await next();
            });

            // Replaces IIS static file handler and the Bundle/Content/Scripts virtual paths
            app.UseStaticFiles();

            // Replaces RouteConfig.RegisterRoutes
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            // Process-local (stateless) EF interceptor registration; no shared-state initialization per instance
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());
        }
    }
}
