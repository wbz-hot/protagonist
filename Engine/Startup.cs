using System;
using System.Collections.Generic;
using Amazon.SQS;
using DLCS.Model.Assets;
using DLCS.Repository;
using Engine.Infrastructure;
using Engine.Ingest;
using Engine.Ingest.Workers;
using Engine.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            services.Configure<QueueSettings>(configuration.GetSection("Queues"));
            
            services
                .AddCors()
                .AddDefaultAWSOptions(configuration.GetAWSOptions())
                .AddSQSSubscribers();

            services
                .AddControllers()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddNewtonsoftJson();

            services
                .AddTransient<ImageIngesterWorker>()
                .AddTransient<TimebasedIngesterWorker>()
                .AddTransient<AssetIngester>()
                .AddTransient<IngestorResolver>(provider => family => family switch
                {
                    AssetFamily.Image => (AssetIngesterWorker) provider.GetService<ImageIngesterWorker>(),
                    AssetFamily.Timebased => provider.GetService<TimebasedIngesterWorker>(),
                    AssetFamily.File => throw new NotImplementedException("File shouldn't be here"),
                    _ => throw new KeyNotFoundException()
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting()
                .UseCors()
                .UseHealthChecks("/ping")
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}