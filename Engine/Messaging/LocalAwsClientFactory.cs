using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using DLCS.Core.Guard;
using Engine.Settings;
using JustSaying.AwsTools;

namespace Engine.Messaging
{
    /// <summary>
    /// <see cref="IAwsClientFactory"/> implementation that uses local mock-SQS for debugging.
    /// See readme - this needs _something_ running locally to use.
    /// </summary>
    public class LocalAwsClientFactory : IAwsClientFactory
    {
        private readonly AWSCredentials credentials;
        private readonly QueueSettings queueSettings;

        public LocalAwsClientFactory(QueueSettings queueSettings, AWSCredentials credentials)
        {
            this.queueSettings = queueSettings;
            this.credentials = credentials;
        }

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
            => new AmazonSimpleNotificationServiceClient(
                credentials,
                new AmazonSimpleNotificationServiceConfig
                {
                    ServiceURL = queueSettings.LocalRoot.ThrowIfNullOrWhiteSpace(nameof(queueSettings.LocalRoot))
                });

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
            => new AmazonSQSClient(
                credentials,
                new AmazonSQSConfig
                {
                    ServiceURL = queueSettings.LocalRoot.ThrowIfNullOrWhiteSpace(nameof(queueSettings.LocalRoot))
                });
    }
}