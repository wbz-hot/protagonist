using System;
using System.Collections.Generic;
using System.Net.Http;
using Amazon.SQS;
using AutoMapper;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Policies;
using DLCS.Model.Security;
using DLCS.Repository;
using DLCS.Repository.Assets;
using DLCS.Repository.Customer;
using DLCS.Repository.Policies;
using DLCS.Repository.Security;
using DLCS.Web.Handlers;
using Engine.Ingest;
using Engine.Ingest.Completion;
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
                .AddTransient<IPolicyRepository, PolicyRepository>()
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
                .AddSingleton<IOriginStrategy, DefaultOriginStrategy>()
                .AddSingleton<IOriginStrategy, BasicHttpAuthOriginStrategy>()
                .AddTransient<IOriginStrategy, SftpOriginStrategy>()
                .AddTransient<RequestTimeLoggingHandler>()
                .AddSingleton<IIngestorCompletion, ImageIngestorCompletion>();

            // image-processor gets httpClient for calling appetiser/tizer
            services
                .AddHttpClient<IImageProcessor, ImageProcessor>(client =>
                    {
                        client.BaseAddress = engineSettings.ImageIngest.ImageProcessorUrl;
                        client.Timeout = TimeSpan.FromMilliseconds(engineSettings.ImageIngest.ImageProcessorTimeoutMs);
                    })
                .AddHttpMessageHandler<RequestTimeLoggingHandler>();
            
            // Add a named httpClient for fetching from origins
            services
                .AddHttpClient(HttpClients.OriginStrategy, client =>
                {
                    client.DefaultRequestHeaders.Add("Accept", "*/*");
                    client.DefaultRequestHeaders.Add("User-Agent", "DLCS/2.0");
                })
                .AddHttpMessageHandler<RequestTimeLoggingHandler>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 8
                });

            services
                .AddHttpClient<OrchestratorClient>(client =>
                {
                    client.BaseAddress = engineSettings.OrchestratorBaseUrl;
                    client.Timeout = TimeSpan.FromMilliseconds(engineSettings.OrchestratorTimeoutMs);
                })
                .AddHttpMessageHandler<RequestTimeLoggingHandler>();

            return services;
        }
    }
}