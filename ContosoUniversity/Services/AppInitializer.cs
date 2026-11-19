using ContosoUniversity.DAL;
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// One-time, cluster-wide initialization (database migrations / seed). Replaces work that used to
    /// run in Global.asax Application_Start on every instance. Run it once as a Cloud Run init step
    /// (e.g. Cloud Run Job / init container) by starting the app with INIT_ONLY=true; the process
    /// exits after initialization completes. Regular serving instances never run it.
    /// </summary>
    public static class AppInitializer
    {
        public static bool IsInitOnly =>
            string.Equals(Environment.GetEnvironmentVariable("INIT_ONLY"), "true", StringComparison.OrdinalIgnoreCase);

        public static void RunOneTimeInitialization()
        {
            var configuration = new Migrations.Configuration();
            var migrator = new DbMigrator(configuration);
            migrator.Update();
        }
    }
}
