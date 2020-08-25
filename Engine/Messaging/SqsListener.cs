using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Engine.Ingest;
using Engine.Messaging.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Engine.Messaging
{
    /// <summary>
    /// Subscribes to SQS, using long polling to receive messages
    /// </summary>
    public class SqsListener
    {
        private readonly IAmazonSQS client;
        private readonly SubscribedToQueue queue;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<SqsListener> logger;

        // if that differs IngestHandler will need to be something smarter
        public SqsListener(IAmazonSQS client, SubscribedToQueue queue,
            IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
        {
            this.client = client;
            this.queue = queue;
            this.serviceScopeFactory = serviceScopeFactory;
            logger = loggerFactory.CreateLogger<SqsListener>();
        }

        /// <summary>
        /// Get value checking if listener is currently listening to a queue.
        /// </summary>
        public bool IsListening { get; private set; }

        /// <summary>
        /// Start listening to configured queue.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken for listen request.</param>
        public void Listen(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            
            // kick off listener loop in the background
            _ = Task.Run(async () =>
            {
                await ListenLoop(cancellationToken);
                IsListening = false;
                logger.LogInformation("Stopped listening to queue {queueName} at {queueUrl}", queue.Name,
                    queue.Url);
            });

            IsListening = true;
            logger.LogInformation("Started listening to queue {queueName} at {queueUrl}", queue.Name, queue.Url);
        }

        private async Task ListenLoop(CancellationToken cancellationToken)
        {
            // TODO - handle X message at a time and make timeout configurable
            while (!cancellationToken.IsCancellationRequested)
            {
                ReceiveMessageResponse response = null;
                int messageCount = 0;
                try
                {
                    response = await GetMessagesFromQueue(cancellationToken);
                    messageCount = response?.Messages?.Count ?? 0;
                    logger.LogTrace("Polled for messages on queue {queueName} and received {messageCount} messages",
                        queue.Name, messageCount);
                }
                catch (Exception ex)
                {
                    // TODO - are there any specific issues to handle rather than generic? 
                    logger.LogError(ex, "Error receiving messages on queue {queueName}", queue.Name);
                }

                if (messageCount == 0) continue;

                try
                {
                    foreach (var message in response!.Messages!)
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        var processed = await HandleMessage(message, cancellationToken);

                        if (processed)
                        {
                            await DeleteMessage(message, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in listen loop for queue {queueName}", queue.Name);
                }
            }
        }

        private Task<ReceiveMessageResponse> GetMessagesFromQueue(CancellationToken cancellationToken)
        {
            // TODO - this is where we can manage throttling of response.
            // Make sure there are enough workers
            var request = new ReceiveMessageRequest
            {
                QueueUrl = queue.Url,
                WaitTimeSeconds = 20 // TODO - paramaterise
            };

            return client.ReceiveMessageAsync(request, cancellationToken);
        }

        private async Task<bool> HandleMessage(Message message, CancellationToken cancellationToken)
        {
            try
            {
                // TODO verify w/ md5?
                var queueMessage = new QueueMessage {Attributes = message.Attributes, Body = message.Body};
                
                // create a new scope to avoid issues with Scoped dependencies
                using var listenerScope = serviceScopeFactory.CreateScope();
                var handlerResolver = listenerScope.ServiceProvider.GetService<QueueHandlerResolver>();
                var handler = handlerResolver(queue);
                
                var processed = await handler.Handle(queueMessage, cancellationToken);
                return processed;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling message {messageId} from queue {queueName}", queue.Name,
                    message.MessageId);
                return false;
            }
        }
        
        private Task DeleteMessage(Message message, CancellationToken cancellationToken)
            => client.DeleteMessageAsync(new DeleteMessageRequest
            {
                QueueUrl = queue.Url,
                ReceiptHandle = message.ReceiptHandle
            }, cancellationToken);
    }
}