using ContosoUniversity.Application.Interfaces;
using ContosoUniversity.Application.Services;
using ContosoUniversity.Domain.Interfaces;
using ContosoUniversity.Infrastructure.Data;
using ContosoUniversity.Infrastructure.Logging;
using ContosoUniversity.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();

// Configure EF Core with SQL Server
// Replaces: web.config entityFramework section + SchoolContext constructor
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SchoolContext")
        ?? "Server=(localdb)\\mssqllocaldb;Database=ContosoUniversity;Trusted_Connection=True;MultipleActiveResultSets=true"));

// Register repositories (replaces direct DbContext usage in controllers)
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IInstructorRepository, InstructorRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();

// Register application services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IInstructorService, InstructorService>();

// Register logging (replaces legacy Trace-based Logger)
builder.Services.AddScoped<IAppLogger, AppLogger>();

var app = builder.Build();

// Seed the database on startup (replaces legacy SchoolInitializer / DbMigrationsConfiguration.Seed)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SchoolContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline
// Replaces: web.config system.web / system.webServer sections
if (!app.Environment.IsDevelopment())
{
    // Replaces: FilterConfig.RegisterGlobalFilters with HandleErrorAttribute
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files from wwwroot (replaces /Content, /Scripts, /fonts)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Replaces: RouteConfig.RegisterRoutes with default route
// Replaces: routes.MapRoute(name: "Default", url: "{controller}/{action}/{id}", ...)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
