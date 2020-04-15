using System;
using System.Collections.Generic;
using Amazon.SQS;
using DLCS.Model.Assets;
using Engine.Ingest;
using Engine.Ingest.Strategy;
using Engine.Ingest.Workers;
using Engine.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Infrastructure
{
    /// <summary>
    /// Extension methods for working with <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionX
    {
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
        /// <returns>Modified <see cref="IServiceCollection"/> object.</returns>
        public static IServiceCollection AddAssetIngestion(this IServiceCollection services)
            => services
                .AddTransient<ImageIngesterWorker>()
                .AddTransient<TimebasedIngesterWorker>()
                .AddTransient<AssetIngester>()
                .AddTransient<IngestorResolver>(provider => family => family switch
                {
                    AssetFamily.Image => (AssetIngesterWorker) provider.GetService<ImageIngesterWorker>(),
                    AssetFamily.Timebased => provider.GetService<TimebasedIngesterWorker>(),
                    AssetFamily.File => throw new NotImplementedException("File shouldn't be here"),
                    _ => throw new KeyNotFoundException()
                })
                .AddTransient<IAssetFetcher, AssetFetcher>()
                .Scan(scan => scan
                    .FromCallingAssembly()
                    .AddClasses(classes => classes.AssignableTo<IOriginStrategy>())
                    .AsImplementedInterfaces()
                    .WithTransientLifetime());
        // TODO - verify lifecycles - make singletons?
    }
}