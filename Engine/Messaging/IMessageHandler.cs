using System.Threading;
using System.Threading.Tasks;
using Engine.Messaging.Models;

namespace Engine.Messaging
{
    /// <summary>
    /// Delegate for getting <see cref="IMessageHandler"/> for queue.
    /// </summary>
    /// <param name="queue">Queue message was received on.</param>
    public delegate IMessageHandler QueueHandlerResolver(SubscribedToQueue queue);

    /// <summary>
    /// Enum representing the different types of queue.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Queue used for ingesting assets.
        /// </summary>
        Ingest = 0,
        
        /// <summary>
        /// Queue for responding to Transcode completion events
        /// </summary>
        TranscodeComplete = 1
    }
    
    /// <summary>
    /// Base interface for consuming messages from queues.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// The type of message that this handler handles.
        /// </summary>
        MessageType Type { get; }
        
        /// <summary>
        /// Handle message from queue, returning bool representing success/failure to handle.
        /// </summary>
        /// <param name="message">Message from queue.</param>
        /// <returns>True if message handled successfully, else false.</returns>
        Task<bool> Handle(QueueMessage message, CancellationToken cancellationToken);
    }
}