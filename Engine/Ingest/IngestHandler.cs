using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Models;
using Engine.Messaging;
using Engine.Messaging.Models;
using Newtonsoft.Json;

namespace Engine.Ingest
{
    /// <summary>
    /// Handler for ingest messages that have been pulled from queue.
    /// </summary>
    public class IngestHandler : IMessageHandler
    {
        private readonly AssetIngester ingester;
        
        public IngestHandler(AssetIngester ingester)
        {
            this.ingester = ingester;
        }
        
        public async Task<bool> Handle(QueueMessage message, CancellationToken cancellationToken)
        {
            var request = JsonConvert.DeserializeObject<IncomingIngestEvent>(message.Body);
            
            var internalIngestRequest = await ingester.Ingest(request, cancellationToken);

            return internalIngestRequest == IngestResult.Success ||
                   internalIngestRequest == IngestResult.QueuedForProcessing;
        }
    }
}