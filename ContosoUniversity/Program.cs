using ContosoUniversity.DAL;
using ContosoUniversity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// MVC with global anti-forgery validation for all unsafe HTTP verbs (replaces per-action reliance on defaults).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Entity Framework Core (replaces EF6 + web.config connection factory).
var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
    ?? throw new InvalidOperationException("Connection string 'SchoolContext' was not found.");

builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 6));
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>());
});

// Application Insights (replaces the Application Insights Web SDK HTTP module).
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// Apply migrations and seed on startup (never drops an existing database).
if (builder.Configuration.GetValue("Database:InitializeOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        await context.Database.MigrateAsync();
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// NOTE: The original application had no authentication; all endpoints remain anonymous.
// Add AddAuthentication().AddCookie() and [Authorize] attributes if access control is required.
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
