using System.Threading;
using System.Threading.Tasks;
using Engine.Messaging;
using Engine.Messaging.Models;

namespace Engine.Ingest.Handlers
{
    /// <summary>
    /// Handler for Transcode Complete messages. 
    /// </summary>
    public class TranscodeCompleteHandler : IMessageHandler
    {
        public MessageType Type => MessageType.TranscodeComplete;
        
        public Task<bool> Handle(QueueMessage message, CancellationToken cancellationToken)
        {
            // Move assets from elastic transcoder-output bucket to main bucket
            // set Width and Height
            // mark DB records as done

            return Task.FromResult(true);
        }
    }
}