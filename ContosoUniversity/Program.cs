using ContosoUniversity.DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Equivalent of the global anti-forgery protection on state-changing requests.
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
        ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>());
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while creating or seeding the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
