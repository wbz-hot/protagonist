using System.Threading;
using System.Threading.Tasks;
using Amazon.Extensions.NETCore.Setup;
using Engine.Messaging;
using Engine.Settings;
using JustSaying;
using JustSaying.AwsTools;
using JustSaying.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine
{
    /// <summary>
    /// <see cref="IHostedService"/> implementation for subscribing/unsubscribing to message queues.
    /// </summary>
    public class ManageSQSSubscriptionsService : IHostedService
    {
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly IHandlerResolver handlerResolver;
        private readonly QueueSettings queueSettings;
        private readonly ILogger<ManageSQSSubscriptionsService> logger;
        private readonly IMayWantOptionalSettings sqsSettings;

        public ManageSQSSubscriptionsService(
            IHostApplicationLifetime hostApplicationLifetime,
            AWSOptions awsOpts,
            ILoggerFactory loggerFactory,
            IOptions<QueueSettings> queueSettings,
            IHandlerResolver handlerResolver)
        {
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.handlerResolver = handlerResolver;
            this.queueSettings = queueSettings.Value;
            logger = loggerFactory.CreateLogger<ManageSQSSubscriptionsService>();
            
            CreateMeABus.DefaultClientFactory = () => new DefaultAwsClientFactory(awsOpts.Credentials);
            sqsSettings = CreateMeABus.WithLogging(loggerFactory)
                .InRegion(awsOpts.Region.SystemName);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Hosted service StartAsync");
            
            ConfigureSQSListener<IngestImageMessage>(queueSettings.Image);
            ConfigureSQSListener<IngestImageMessage>(queueSettings.ImagePriority);
            ConfigureSQSListener<IngestImageMessage>(queueSettings.Video, true);
            sqsSettings.StartListening();

            hostApplicationLifetime.ApplicationStopping.Register(OnStopping);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Hosted service StopAsync");
            return Task.CompletedTask;
        }

        private void ConfigureSQSListener<T>(string queueName, bool isVideo = false)
            where T : Message
        {
            if (string.IsNullOrWhiteSpace(queueName)) return;

            var fluentSubscription = sqsSettings
                .WithSqsTopicSubscriber()
                .IntoQueue(queueName);

            // TODO - max this configurable?
            if (isVideo)
                fluentSubscription =
                    fluentSubscription.ConfigureSubscriptionWith(configuration =>
                        configuration.MaxAllowedMessagesInFlight = 1);

            fluentSubscription.WithMessageHandler<T>(handlerResolver);
        }

        private void OnStopping()
        {
            // in ECS this will be received 20s before kill command
            if (sqsSettings.Listening) sqsSettings.StopListening();
            logger.LogInformation("Stopping listening to queues");
        }
    }

}