using Amazon.S3;
using DLCS.Model.Storage;
using DLCS.Repository;
using DLCS.Repository.Assets;
using DLCS.Repository.Storage.S3;
using Engine.Infrastructure;
using Engine.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Engine
{
    public class Startup
    {
        private readonly IConfiguration configuration;
        
        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddNpgSql(configuration.GetPostgresSqlConnection());

            var engineSection = configuration.GetSection("Engine");
            var engineSettings = engineSection.Get<EngineSettings>();
            services.Configure<EngineSettings>(engineSection);
            
            services.Configure<QueueSettings>(configuration.GetSection("Queues"));
            

            services
                .AddAWSService<IAmazonS3>()
                .AddSingleton<IBucketReader, S3BucketReader>()
                .AddCors()
                .AddLazyCache()
                .AddDefaultAWSOptions(configuration.GetAWSOptions())
                .AddSQSSubscribers()
                .AddAssetIngestion(engineSettings)
                .AddScoped<IThumbLayoutManager, ThumbLayoutManager>()
                .AddDataAccess();

            services
                .AddControllers()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)ht
                .AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting()
                .UseSerilogRequestLogging()
                .UseCors()
                .UseHealthChecks("/ping")
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}