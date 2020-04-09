using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Engine.Ingest;
using Engine.Messaging.Models;
using Microsoft.Extensions.Logging;

namespace Engine.Messaging
{
    /// <summary>
    /// Manages a collection of SQS listeners.
    /// </summary>
    public class SqsListenerManager
    {
        private readonly IAmazonSQS client;
        private readonly QueueHandlerResolver handlerResolver;
        private readonly ConcurrentBag<SqsListener> listeners;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<SqsListenerManager> logger;
        private readonly object syncRoot = new object();

        public SqsListenerManager(IAmazonSQS client, QueueHandlerResolver handlerResolver, ILoggerFactory loggerFactory)
        {
            this.client = client;
            this.handlerResolver = handlerResolver;
            this.loggerFactory = loggerFactory;
            logger = loggerFactory.CreateLogger<SqsListenerManager>();
            listeners = new ConcurrentBag<SqsListener>();
            cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Configure listener for specified queue. This configures only, doesn't start listening.
        /// </summary>
        /// <param name="queueName">Name of queue to listen to.</param>
        /// <exception cref="InvalidOperationException">Thrown if queue does not exist.</exception>
        public async Task AddQueueListener(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName)) return;

            var queue = new SubscribedToQueue(queueName);
            if (!await VerifyQueueExists(queue))
            {
                logger.LogWarning("Cannot listen to queue '{queueName}' as it does not exist", queue.Name);
                throw new InvalidOperationException($"Cannot listen to queue {queueName} as it does not exist");
            }

            var listener = new SqsListener(client, queue, handlerResolver, loggerFactory);
            listeners.Add(listener);
        }

        /// <summary>
        /// Start listening to all configured queues.
        /// </summary>
        public void StartListening()
        {
            if (cancellationTokenSource.IsCancellationRequested) return;

            lock (syncRoot)
            {
                foreach (var listener in listeners)
                {
                    if (listener.IsListening) continue;
                    
                    listener.Listen(cancellationTokenSource.Token);
                }
            }
        }

        /// <summary>
        /// Signal all queue listeners to stop listening.
        /// </summary>
        public void StopListening()
        {
            if (!cancellationTokenSource.IsCancellationRequested) cancellationTokenSource.Cancel();
        }

        private async Task<bool> VerifyQueueExists(SubscribedToQueue queue)
        {
            GetQueueUrlResponse result;
            
            try
            {
                result = await client.GetQueueUrlAsync(queue.Name);
            }
            catch (QueueDoesNotExistException)
            {
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
}