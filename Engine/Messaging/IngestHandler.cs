using System;
using System.Threading;
using System.Threading.Tasks;
using Engine.Messaging.Models;

namespace Engine.Messaging
{
    /// <summary>
    /// Handler for messages that have been pulled from queue.
    /// </summary>
    public class IngestHandler
    {
        // This is injected so should get anything from DI container injected
        
        public Task<bool> Handle(QueueMessage message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}