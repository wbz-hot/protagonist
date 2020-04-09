using System.Threading;
using System.Threading.Tasks;
using Engine.Messaging.Models;

namespace Engine.Messaging
{
    /// <summary>
    /// Delegate for getting <see cref="IQueueHandler"/> for queue.
    /// </summary>
    /// <param name="queue">Queue message was received on.</param>
    public delegate IQueueHandler QueueHandlerResolver(SubscribedToQueue queue);
    
    /// <summary>
    /// Base interface for consuming messages from queues.
    /// </summary>
    public interface IQueueHandler
    {
        /// <summary>
        /// Handle message from queue, returning bool representing success/failure to handle.
        /// </summary>
        /// <param name="message">Message from queue.</param>
        /// <returns>True if message handled successfully, else false.</returns>
        Task<bool> Handle(QueueMessage message, CancellationToken cancellationToken);
    }
}