using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using ContosoUniversity.Services;
using System;

namespace ContosoUniversity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Centralised config from GCP Secret Manager (consistent across all scaled instances)
            GcpSecretManagerLoader.LoadIntoEnvironment();

            // Init-container / Cloud Run Job mode: run one-time initialization once, then exit.
            if (AppInitializer.IsInitOnly)
            {
                AppInitializer.RunOneTimeInitialization();
                return;
            }

            CreateHostBuilder(args).Build().Run();
        }

        // Kestrel-hosted (no IIS); listens on the PORT env var provided by GKE/Cloud Run, default 8080.
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
                    webBuilder.UseUrls("http://0.0.0.0:" + port);
                    webBuilder.UseStartup<Startup>();
                });
    }
}
