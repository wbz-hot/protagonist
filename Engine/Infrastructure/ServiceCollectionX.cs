using System;
using System.Collections.Generic;
using System.Net.Http;
using Amazon.SQS;
using AutoMapper;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Security;
using DLCS.Repository;
using DLCS.Repository.Assets;
using DLCS.Repository.Security;
using DLCS.Web.Handlers;
using Engine.Ingest;
using Engine.Ingest.Image;
using Engine.Ingest.Strategy;
using Engine.Ingest.Workers;
using Engine.Messaging;
using Engine.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Infrastructure
{
    /// <summary>
    /// Extension methods for working with <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionX
    {
        /// <summary>
        /// Add services for data access.
        /// </summary>
        /// <param name="services">Current <see cref="IServiceCollection"/> object.</param>
        /// <returns>Modified <see cref="IServiceCollection"/> object.</returns>
        public static IServiceCollection AddDataAccess(this IServiceCollection services)
            => services
                .AddAutoMapper(typeof(DatabaseConnectionManager))
                .AddTransient<DatabaseAccessor>()
                .AddTransient<ICustomerOriginRepository, CustomerOriginStrategyRepository>()
                .AddTransient<IAssetPolicyRepository, AssetPolicyRepository>()
                .AddTransient<IAssetRepository, AssetRepository>()
                .AddSingleton<ICredentialsRepository, CredentialsRepository>();

        /// <summary>
        /// Add SQS queue handlers to service collection.
        /// </summary>
        /// <param name="services">Current <see cref="IServiceCollection"/> object.</param>
        /// <returns>Modified <see cref="IServiceCollection"/> object.</returns>
        public static IServiceCollection AddSQSSubscribers(this IServiceCollection services)
            => services
                .AddAWSService<IAmazonSQS>()
                .AddSingleton<IngestHandler>()
                .AddSingleton<SqsListenerManager>()
                .AddTransient<QueueHandlerResolver>(provider => queue =>
                    {
                        // TODO - add logic for ElasticTranscoder handling
                        return provider.GetService<IngestHandler>();
                    })
                .AddHostedService<ManageSQSSubscriptionsService>();

        /// <summary>
        /// Adds all <see cref="IAssetIngesterWorker"/> and related dependencies. 
        /// </summary>
        /// <param name="services">Current <see cref="IServiceCollection"/> object.</param>
        /// <param name="engineSettings"></param>
        /// <returns>Modified <see cref="IServiceCollection"/> object.</returns>
        public static IServiceCollection AddAssetIngestion(this IServiceCollection services,
            EngineSettings engineSettings)
        {
            // TODO - if a/v and image ingestion deployed separately there will need to be some logic on registered deps
            services
                .AddTransient<TimebasedIngesterWorker>()
                .AddTransient<ImageIngesterWorker>()
                .AddTransient<AssetIngester>()
                .AddTransient<IngestorResolver>(provider => family => family switch
                {
                    AssetFamily.Image => provider.GetService<ImageIngesterWorker>(),
                    AssetFamily.Timebased => provider.GetService<TimebasedIngesterWorker>(),
                    AssetFamily.File => throw new NotImplementedException("File shouldn't be here"),
                    _ => throw new KeyNotFoundException()
                })
                .AddTransient<IAssetFetcher, AssetFetcher>()
                .AddTransient<IOriginStrategy, S3AmbientOriginStrategy>()
                .AddTransient<IOriginStrategy, SftpOriginStrategy>()
                .AddTransient<RequestTimeLoggingHandler>();

            // image-processor gets httpClient for calling appetiser/tizer
            services
                .AddHttpClient<IImageProcessor, ImageProcessor>(client =>
                    {
                        client.BaseAddress = engineSettings.ImageIngest.ImageProcessorUrl;
                        client.Timeout = TimeSpan.FromMilliseconds(engineSettings.ImageIngest.ImageProcessorTimeoutMs);
                    })
                .AddHttpMessageHandler<RequestTimeLoggingHandler>();
            
            // defaultOriginStrategy gets httpClient for fetching assets via http
            services
                .AddHttpClient<IOriginStrategy, DefaultOriginStrategy>(client => ConfigureDlcsClient(client))
                .AddHttpMessageHandler<RequestTimeLoggingHandler>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 8
                });

            // basicHttpAuthOriginStrategy gets httpClient for fetching assets via http
            services
                .AddHttpClient<IOriginStrategy, BasicHttpAuthOriginStrategy>(client => ConfigureDlcsClient(client))
                .AddHttpMessageHandler<RequestTimeLoggingHandler>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 8
                });

            return services;
        }

        private static HttpClient ConfigureDlcsClient(HttpClient client)
        {
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("User-Agent", "DLCS/2.0");
            return client;
        }
    }
}