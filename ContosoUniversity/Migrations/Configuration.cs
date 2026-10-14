using Microsoft.EntityFrameworkCore.Migrations;

namespace ContosoUniversity.Migrations
{
    // EF Core 8 uses IMigrationAssembly and DesignTimeServices instead of DbMigrationsConfiguration
    // The Seed method has been moved to DAL/SchoolInitializer.cs (SeedData.Initialize)
    // This class is kept for reference but EF Core migrations are configured via Program.cs
    internal sealed class Configuration
    {
        public Configuration()
        {
            // AutomaticMigrationsEnabled = false; // EF Core does not support automatic migrations
        }
    }
}
