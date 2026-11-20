using ContosoUniversity.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC with views (replaces System.Web.Mvc registration in Global.asax).
builder.Services.AddControllersWithViews();

// Application Insights (replaces the classic ApplicationInsightsHttpModule). Enabled only when configured.
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]) ||
    !string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddApplicationInsightsTelemetry(builder.Configuration);
}

// EF Core data access with SQL logging interceptor and connection resiliency
// (replaces SchoolConfiguration / SqlAzureExecutionStrategy and DbInterception.Add).
builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>());
});

var app = builder.Build();

// Create/upgrade the database and seed it (replaces the EF6 database initializer).
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();

        if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
        {
            await context.Database.MigrateAsync();
        }

        if (app.Configuration.GetValue("Database:SeedOnStartup", true))
        {
            await DbInitializer.InitializeAsync(context);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Error handling (replaces HandleErrorAttribute).
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Order matters: authentication must precede authorization. No auth schemes are configured
// because the legacy application had none.
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
