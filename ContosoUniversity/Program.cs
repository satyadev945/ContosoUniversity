using System;
using ContosoUniversity.DAL;
using ContosoUniversity.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register interceptors (replaces DbInterception.Add)
var loggingInterceptor = new SchoolInterceptorLogging();
var transientErrorsInterceptor = new SchoolInterceptorTransientErrors();

// Configure EF Core with retry execution strategy (replaces EF6 SqlAzureExecutionStrategy)
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure())
           .AddInterceptors(loggingInterceptor, transientErrorsInterceptor));

// Register logger
builder.Services.AddSingleton<ILogger>(new Logger());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize database and seed data (replaces EF6 Database.SetInitializer)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SchoolContext>();
        context.Database.Migrate();
        SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger>();
        logger.Error(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
