using System.Diagnostics;
using DLCS.Core.Guard;

namespace Engine.Messaging.Models
{
    /// <summary>
    /// Model representing a queue that has been subscribed to.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class SubscribedToQueue
    {
        /// <summary>
        /// Get the name of this queue
        /// </summary>
        public string Name { get;  }

        /// <summary>
        /// Get the type of message this queue handles
        /// </summary>
        public MessageType MessageType { get; }

        /// <summary>
        /// Get the full URL of this queue
        /// </summary>
        public string? Url { get; private set; }

        private string DebuggerDisplay => $"{Name} - {Url}";

        public SubscribedToQueue(string queueName, MessageType messageType)
        {
            Name = queueName.ThrowIfNullOrWhiteSpace(nameof(queueName));
            MessageType = messageType;
        }
        
        // TODO - have throttle levels in here?

        public void SetUri(string queueUri) => Url = queueUri;
    }
}