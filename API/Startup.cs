using System.Security.Claims;
using Amazon.S3;
using API.Auth;
using API.Client;
using API.Infrastructure;
using API.Settings;
using DLCS.AWS.Configuration;
using DLCS.AWS.S3;
using DLCS.Core.Encryption;
using DLCS.Model;
using DLCS.Model.Auth;
using DLCS.Model.Customers;
using DLCS.Model.Processing;
using DLCS.Repository;
using DLCS.Repository.Auth;
using DLCS.Repository.Caching;
using DLCS.Repository.Customers;
using DLCS.Repository.Entities;
using DLCS.Web.Auth;
using DLCS.Web.Configuration;
using Hydra;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;

namespace API
{
    public class Startup
    {
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;

        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ApiSettings>(configuration);
            var cachingSection = configuration.GetSection("Caching");
            services.Configure<CacheSettings>(cachingSection);
            
            var apiSettings = configuration.Get<ApiSettings>();
            var cacheSettings = cachingSection.Get<CacheSettings>();
            
            services.AddHttpClient();

            services
                .AddHttpContextAccessor()
                .AddSingleton<IEncryption, SHA256>()
                .AddSingleton<DeliveratorApiAuth>()
                .AddTransient<ClaimsPrincipal>(s => s.GetService<IHttpContextAccessor>().HttpContext.User)
                .AddMemoryCache(memoryCacheOptions =>
                {
                    memoryCacheOptions.SizeLimit = cacheSettings.MemoryCacheSizeLimit;
                    memoryCacheOptions.CompactionPercentage = cacheSettings.MemoryCacheCompactionPercentage;
                })
                .AddLazyCache()
                .AddSingleton<ICustomerRepository, DapperCustomerRepository>()
                .AddScoped<IEntityCounterRepository, EntityCounterRepository>()
                .AddSingleton<IAuthServicesRepository, DapperAuthServicesRepository>()
                .AddScoped<ICustomerQueueRepository, CustomerQueueRepository>()
                .ConfigureMediatR()
                .ConfigureSwagger()
                .AddDlcsContext(configuration);

            services
                .AddSingleton<IBucketReader, S3BucketReader>()
                .AddSingleton<IBucketWriter, S3BucketWriter>()
                .AddSingleton<IStorageKeyGenerator, S3StorageKeyGenerator>()
                .SetupAWS(configuration, webHostEnvironment)
                .WithAmazonS3();

            services.AddDlcsDelegatedBasicAuth(options =>
                {
                    options.Realm = "DLCS-API";
                    options.Salt = apiSettings.Salt;
                });
            
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
                .AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ApplyHydraSerializationSettings();
                });

            services
                .AddHealthChecks()
                .AddUrlGroup(apiSettings.DLCS.ApiRoot, "DLCS API");
            
            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 100_000_000; // if don't set default value is: 30 MB
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var applicationOptions = configuration.Get<ApiSettings>();
            var pathBase = applicationOptions.PathBase;

            app
                .HandlePathBase(pathBase, logger)
                .UseSwaggerWithUI("DLCS API", pathBase, "v2")
                .UseRouting()
                .UseSerilogRequestLogging()
                .UseCors("CorsPolicy")
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints
                        .MapControllers()
                        .RequireAuthorization();
                    endpoints.MapHealthChecks("/ping").AllowAnonymous();
                });
        }
    }
}