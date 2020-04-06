using DLCS.Repository;
using Engine.Settings;
using JustSaying;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using JustSaying.Models;

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
            services.AddCors();
            services.AddDefaultAWSOptions(configuration.GetAWSOptions());
            services.AddSingleton<IHandlerResolver, DlcsHandlerResolver>();
            services.AddHostedService<ManageSQSSubscriptionsService>();

            services
                .AddControllers()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddNewtonsoftJson();
            
            services.Configure<QueueSettings>(configuration.GetSection("Queues"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors();
            app.UseHealthChecks("/ping");

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}