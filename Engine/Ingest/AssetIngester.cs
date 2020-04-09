using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using Engine.Ingest.Models;
using Engine.Ingest.Workers;
using Engine.Messaging.Models;

namespace Engine.Ingest
{
    /// <summary>
    /// Delegate for getting the correct <see cref="AssetIngesterWorker"/> for specified Family.
    /// </summary>
    /// <param name="family">The type of ingester worker.</param>
    public delegate AssetIngesterWorker IngestorResolver(AssetFamily family);

    /// <summary>
    /// Contains operations for ingesting assets.
    /// </summary>
    public class AssetIngester
    {
        private readonly IngestorResolver resolver;

        public AssetIngester(IngestorResolver resolver)
        {
            this.resolver = resolver;
        }
        
        /// <summary>
        /// Run ingest based on <see cref="IncomingIngestEvent"/>.
        /// </summary>
        /// <returns>Result of ingest operations</returns>
        /// <remarks>This is to comply with message format sent by Deliverator API.</remarks>
        public Task<IngestResult> Ingest(IncomingIngestEvent request, CancellationToken cancellationToken)
        {
            var internalIngestRequest = request.ConvertToInternalRequest();
            return Ingest(internalIngestRequest, cancellationToken);
        }

        /// <summary>
        /// Run ingest based on <see cref="IngestAssetRequest"/>.
        /// </summary>
        /// <returns>Result of ingest operations</returns>
        public Task<IngestResult> Ingest(IngestAssetRequest request, CancellationToken cancellationToken)
        {
            // TODO - make Family a char for easier conversion? 
            var ingestor = resolver(request.Asset.Family == "I" ? AssetFamily.Image : AssetFamily.Timebase);

             return ingestor.Ingest(request, cancellationToken);
        }
    }
}