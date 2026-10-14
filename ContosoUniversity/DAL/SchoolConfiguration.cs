using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoUniversity.DAL
{
    public static class SchoolConfiguration
    {
        // EF Core 8 configures execution strategy in Program.cs via EnableRetryOnFailure
        // This class is kept for compatibility but the configuration is now done in Program.cs
    }
}
