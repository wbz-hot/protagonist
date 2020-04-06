using System;
using Engine.Messaging;
using JustSaying;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Engine
{
    /// <summary>
    /// <see cref="IHandlerResolver"/> implementation that resolves handler from DI container.
    /// </summary>
    public class DlcsHandlerResolver : IHandlerResolver
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<DlcsHandlerResolver> logger;

        public DlcsHandlerResolver(IServiceProvider serviceProvider, ILogger<DlcsHandlerResolver> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }
        
        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
        {
            if (typeof(T) == typeof(IngestImageMessage))
            {
                return serviceProvider.GetService<IngestMessageHandler>() as IHandlerAsync<T>;
            }
            
            logger.LogError("Attempt to get handler for unknown message type {messageType}", typeof(T));
            throw new NotImplementedException();
        }
    }
}