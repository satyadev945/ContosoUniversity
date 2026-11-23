using System.Threading.Tasks;
using ContosoUniversity.DAL;
using ContosoUniversity.Infrastructure;
using System.Data.Entity.Infrastructure.Interception;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace ContosoUniversity
{
    /// <summary>
    /// Application startup logic. No longer derives from System.Web.HttpApplication
    /// (IIS-only pipeline hook); invoked explicitly from the Kestrel host (see Program.cs)
    /// so the app can run on Amazon ECS/EKS without IIS.
    /// Per-process registrations (routes, filters, EF interceptors) run on every instance;
    /// cluster-wide initialization runs once via AWS Parameter Store + DynamoDB distributed lock.
    /// </summary>
    public class MvcApplication
    {
        public static void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        public static async Task StartAsync()
        {
            // Per-process registrations: required on every instance.
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());

            // Shared initialization: executed exactly once across all scaled instances
            // (state tracked in SSM Parameter Store, coordinated with a DynamoDB lock).
            await DistributedInitializer.RunOnceAsync(() => Task.CompletedTask);
        }
    }
}
