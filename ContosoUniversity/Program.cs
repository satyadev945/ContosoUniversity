using ContosoUniversity;
using ContosoUniversity.Middleware;
using ContosoUniversity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

// Kestrel-hosted entry point replacing the IIS-hosted Global.asax / HttpApplication pipeline.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

// Anti-XSS: HtmlSanitizer (Ganss.Xss) with allow-list policy sourced from AWS Secrets Manager.
// Use IHtmlSanitizerService.Sanitize(...) instead of Html.Raw()/HtmlString for any untrusted HTML.
var sanitizerPolicy = await AwsSecretsHtmlSanitizerPolicyProvider.LoadAsync();
builder.Services.AddSingleton<IHtmlSanitizerService>(new HtmlSanitizerService(sanitizerPolicy));

var app = builder.Build();

// Equivalent of the former Application_Start (EF interceptors, etc.)
await MvcApplication.StartAsync();

// Replaces IIS BlockViewHandler from Views/Web.config
app.UseBlockViewAccess();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
