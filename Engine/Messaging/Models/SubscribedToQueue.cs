using System.Diagnostics;

namespace Engine.Messaging.Models
{
    /// <summary>
    /// Model representing a queue that has been subscribed to.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class SubscribedToQueue
    {
        public string Name { get;  }
        
        public string Url { get; private set; }

        private string DebuggerDisplay => $"{Name} - {Url}";

        public SubscribedToQueue(string queueName)
        {
            Name = queueName;
        }
        
        // TODO - have throttle levels in here?

        public void SetUri(string queueUri) => Url = queueUri;
    }
}