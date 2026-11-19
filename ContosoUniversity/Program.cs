using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;

namespace ContosoUniversity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // Self-hosted on Kestrel (no IIS); listens on the PORT env variable (default 8080) for GKE.
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
                    webBuilder.UseUrls($"http://0.0.0.0:{port}");
                    webBuilder.UseStartup<Startup>();
                });
    }
}
