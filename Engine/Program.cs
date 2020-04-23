using System;
using DLCS.Model.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Engine
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((hostingContext, loggerConfiguration)
                    => loggerConfiguration
                        .ReadFrom.Configuration(hostingContext.Configuration)
                        .Enrich.FromLogContext()
                        .Destructure.ByTransforming<BasicCredentials>(credentials => new {credentials.User})
                )
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var isDevelopment = context.HostingEnvironment.IsDevelopment();
                    builder.AddSystemsManager(configurationSource =>
                    {
                        // TODO - pull this path from config to allow multiple different deployments
                        configurationSource.Path = "/engine/";
                        configurationSource.ReloadAfter = TimeSpan.FromMinutes(90);

                        // Using ParameterStore optional if Development
                        configurationSource.Optional = isDevelopment;
                    });

                    // If development then ensure appsettings.Development.json wins
                    if (isDevelopment)
                    {
                        builder.AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true);
                    }
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}