using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Engine.Messaging.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Engine.Messaging
{
    /// <summary>
    /// Manages a collection of SQS listeners.
    /// </summary>
    public class SqsListenerManager
    {
        private readonly IAmazonSQS client;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ConcurrentBag<SqsListener> listeners;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<SqsListenerManager> logger;
        private readonly object syncRoot = new object();

        public SqsListenerManager(IAmazonSQS client,
            IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
        {
            this.client = client;
            this.serviceScopeFactory = serviceScopeFactory;
            this.loggerFactory = loggerFactory;
            logger = loggerFactory.CreateLogger<SqsListenerManager>();
            listeners = new ConcurrentBag<SqsListener>();
            cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Configure listener for specified queue. This configures only, doesn't start listening.
        /// </summary>
        /// <param name="queueName">Name of queue to listen to.</param>
        /// <param name="messageType">The type of message this queue is for.</param>
        /// <exception cref="InvalidOperationException">Thrown if queue does not exist.</exception>
        public async Task AddQueueListener(string queueName, MessageType messageType)
        {
            if (string.IsNullOrWhiteSpace(queueName)) return;

            var queue = new SubscribedToQueue(queueName, messageType);
            if (!await VerifyQueueExists(queue))
            {
                logger.LogWarning("Cannot listen to queue '{queueName}' as it does not exist", queue.Name);
                throw new InvalidOperationException($"Cannot listen to queue {queueName} as it does not exist");
            }

            logger.LogInformation("Listener configured for '{queueName}' at '{queueUrl}'", queueName, queue.Url);
            var listener = new SqsListener(client, queue, serviceScopeFactory, loggerFactory);
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
            catch (QueueDoesNotExistException qEx)
            {
                logger.LogError(qEx, "Attempt to listen to queue '{queue}' but it doesn't exist", queue.Name);
                return false;
            }
            catch (Exception e)
            {
                logger.LogError(e, "General error attempting to listen to queue '{queue}'", queue.Name);
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