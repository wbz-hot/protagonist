using Amazon.S3;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.PathElements;
using DLCS.Model.Storage;
using DLCS.Repository;
using DLCS.Repository.Assets;
using DLCS.Repository.Settings;
using DLCS.Repository.Storage.S3;
using DLCS.Web.Configuration;
using DLCS.Web.Requests.AssetDelivery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orchestrator.Images;
using Orchestrator.Settings;
using Serilog;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Orchestrator
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
            services.Configure<OrchestratorSettings>(configuration);
            services.Configure<ThumbsSettings>(configuration.GetSection("Thumbs"));
            
            services
                .AddLazyCache()
                .AddSingleton<ICustomerRepository, CustomerRepository>()
                .AddSingleton<IPathCustomerRepository, CustomerPathElementRepository>()
                .AddSingleton<IAssetRepository, AssetRepository>()
                .AddSingleton<AssetDeliveryPathParser>()
                .AddSingleton<ImageRequestHandler>()
                .AddAWSService<IAmazonS3>()
                .AddSingleton<IBucketReader, BucketReader>()
                .AddSingleton<IThumbReorganiser, NonOrganisingReorganiser>()
                .AddSingleton<IThumbRepository, ThumbRepository>();

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .SetIsOriginAllowed(host => true)
                        .AllowCredentials());
            });

            services
                .AddHealthChecks()
                .AddNpgSql(configuration.GetPostgresSqlConnection());
            
            // Add the reverse proxy to capability to the server
            var proxyBuilder = services
                .AddReverseProxy()
                .LoadFromConfig(configuration.GetSection("ReverseProxy"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            var applicationOptions = configuration.Get<OrchestratorSettings>();
            var pathBase = applicationOptions.PathBase;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .HandlePathBase(pathBase, logger)
                .UseHttpsRedirection()
                .UseRouting()
                .UseSerilogRequestLogging()
                .UseCors("CorsPolicy")
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapReverseProxy();
                    endpoints.MapImageHandling();
                    endpoints.MapHealthChecks("/health");
                });
        }
    }
    
    
    public class Thing : ITransformProvider
    {
        public void ValidateRoute(TransformRouteValidationContext context)
        {
            throw new System.NotImplementedException();
        }

        public void ValidateCluster(TransformClusterValidationContext context)
        {
            throw new System.NotImplementedException();
        }

        public void Apply(TransformBuilderContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}