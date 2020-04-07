using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Engine.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Messaging
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
            await sqsListener.AddQueue(queueSettings.Image);
            await sqsListener.AddQueue(queueSettings.ImagePriority);
            await sqsListener.AddQueue(queueSettings.Video);
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

    public class Listener
    {
        private readonly IAmazonSQS client;

        private readonly SubscribedToQueue queue;

        private readonly IngestHandler handler;
        // this is subscribed to a queue. There will be a few of these.
        // they can each be cancelled

        // TODO - this makes the assumption that all handlers will be the same for all queues
        public Listener(IAmazonSQS client, SubscribedToQueue queue, IngestHandler handler)
        {
            this.client = client;
            this.queue = queue;
            this.handler = handler;
        }

        public bool IsListening { get; private set; }

        public void Listen(CancellationToken token)
        {
            // kick off listener loop in the background
            _ = Task.Run(async () =>
            {
                await ListenLoop(token);
                IsListening = false;
                // log stop listen
            });

            IsListening = true;
            // log start listening
        }

        private async Task ListenLoop(CancellationToken token)
        {
            // TODO - how to handle 1 message at a time?
            while (!token.IsCancellationRequested)
            {
                ReceiveMessageResponse response = null;
                try
                {
                    // TODO - do we want to up the number of messages being processed for higher throughput?
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = queue.QueueUrl,
                        WaitTimeSeconds = 20 // TODO - paramaterise
                    };

                    response = await client.ReceiveMessageAsync(request, token);
                }
                catch (Exception ex)
                {
                    // unable to fetch messages, bail
                }

                if (response == null || response.Messages.Count == 0) continue;

                try
                {
                    foreach (var message in response.Messages)
                    {
                        if (token.IsCancellationRequested) return;

                        bool processed;
                        try
                        {
                            // TODO - throttle handling 
                            var queueMessage = new QueueMessage
                            {
                                Attributes = message.Attributes, Body = message.Body
                            };
                            processed = await handler.Handle(queueMessage);
                        }
                        catch (Exception ex)
                        {
                            // TODO - handle + log
                            continue;
                        }

                        if (processed)
                        {
                            await client.DeleteMessageAsync(new DeleteMessageRequest
                            {
                                QueueUrl = queue.QueueUrl,
                                ReceiptHandle = message.ReceiptHandle
                            }, token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // TODO - handle + log
                }
            }
        }
    }

    public class SubscribedToQueue
    {
        public string Name { get;  }
        
        public string QueueUrl { get; private set; }

        public SubscribedToQueue(string queueName)
        {
            Name = queueName;
        }
        
        // TODO - handle throttle levels in here?

        public void SetUri(string queueUri) => QueueUrl = queueUri;
    }
    
    public class SqsListenerManager
    {
        private readonly IAmazonSQS client;
        private readonly IngestHandler handler;

        // should this take a cancellationToken? or have Start and Stop on it?
        private List<Listener> listeners;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly object syncRoot = new object();

        public SqsListenerManager(IAmazonSQS client, IngestHandler handler)
        {
            this.client = client;
            this.handler = handler;
            listeners = new List<Listener>();
            cancellationTokenSource = new CancellationTokenSource();
        }
        
        public async Task AddQueue(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName)) return;

            var queue = new SubscribedToQueue(queueName);
            if (!await VerifyQueueExists(queue))
            {
                throw new InvalidOperationException($"Cannot listen to queue {queueName} as it does not exist");
            }

            var listener = new Listener(client, queue, handler);
            listeners.Add(listener);
        }

        public void StartListening()
        {
            if (cancellationTokenSource.IsCancellationRequested) return;

            lock (syncRoot)
            {
                foreach (var listener in listeners)
                {
                    if (!listener.IsListening)
                    {
                        listener.Listen(cancellationTokenSource.Token);
                    }
                }
            }
        }

        public void StopListening() => cancellationTokenSource.Cancel();

        private async Task<bool> VerifyQueueExists(SubscribedToQueue queue)
        {
            GetQueueUrlResponse result;
            
            try
            {
                result = await client.GetQueueUrlAsync(queue.Name);
            }
            catch (QueueDoesNotExistException)
            {
                // TODO - log
                return false;
            }
            
            if (result?.QueueUrl == null)
            {
                return false;
            }
            
            queue.SetUri(result.QueueUrl);
            return true;
        }
    }

    public class IngestHandler
    {
        // This is injected so should get anything from DI container injected
        
        public Task<bool> Handle(QueueMessage message)
        {
            throw new NotImplementedException();
        }
    }
    
    
    public class QueueMessage
    {
        public string Body { get; set; }
    
        public Dictionary<string, string> Attributes { get; set; }
    }
}