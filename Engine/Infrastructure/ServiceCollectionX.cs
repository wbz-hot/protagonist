using Engine.Ingest;
using Engine.Messaging;
using JustSaying;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Infrastructure
{
    /// <summary>
    /// Extension methods for working with <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionX
    {
        /// <summary>
        /// Add JustSaying queue handlers to service collection.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddJustSaying(this IServiceCollection services)
            => services
                .AddSingleton<IHandlerResolver, DlcsHandlerResolver>()
                .AddSingleton<IngestMessageHandler>()
                .AddHostedService<ManageSQSSubscriptionsService>();
    }
}