using Amazon.SQS;
using Engine.Ingest;
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
        /// <param name="services"></param>
        /// <returns></returns>
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
    }
}