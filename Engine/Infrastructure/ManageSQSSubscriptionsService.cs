﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Engine.Messaging;
using Engine.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Infrastructure
{
    /// <summary>
    /// <see cref="IHostedService"/> implementation for subscribing/unsubscribing to message queues.
    /// </summary>
    public class ManageSQSSubscriptionsService : IHostedService
    {
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly QueueSettings queueSettings;
        private readonly ILogger<ManageSQSSubscriptionsService> logger;
        private readonly SqsListenerManager sqsListener;

        public ManageSQSSubscriptionsService(
            IHostApplicationLifetime hostApplicationLifetime,
            SqsListenerManager sqsListener,
            ILoggerFactory loggerFactory,
            IOptions<QueueSettings> queueSettings)
        {
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.sqsListener = sqsListener;
            this.queueSettings = queueSettings.Value;
            logger = loggerFactory.CreateLogger<ManageSQSSubscriptionsService>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Hosted service StartAsync");

            // TODO - throttle video to only 1 message being processed at any given time
            var configureQueues = new List<Task>
            {
                sqsListener.AddQueueListener(queueSettings.Image),
                sqsListener.AddQueueListener(queueSettings.ImagePriority),
                sqsListener.AddQueueListener(queueSettings.Video)
            };
            await Task.WhenAll(configureQueues);
            sqsListener.StartListening();
            
            hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Hosted service StopAsync");
            return Task.CompletedTask;
        }

        private void OnStopping()
        {
            // in ECS this will be received 20s before kill command
            // TODO - have an isstopping on this?
            sqsListener.StopListening();
            logger.LogInformation("Stopping listening to queues");
        }
    }
}