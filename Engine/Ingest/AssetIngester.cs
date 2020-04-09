using System;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using Engine.Ingest.Models;
using Engine.Ingest.Workers;
using Microsoft.Extensions.Logging;

namespace Engine.Ingest
{
    /// <summary>
    /// Delegate for getting the correct <see cref="IAssetIngesterWorker"/> for specified Family.
    /// </summary>
    /// <param name="family">The type of ingester worker.</param>
    public delegate IAssetIngesterWorker IngestorResolver(AssetFamily family);

    /// <summary>
    /// Contains operations for ingesting assets.
    /// </summary>
    public class AssetIngester
    {
        private readonly IngestorResolver resolver;
        private readonly ILogger<AssetIngester> logger;

        public AssetIngester(IngestorResolver resolver, ILogger<AssetIngester> logger)
        {
            this.resolver = resolver;
            this.logger = logger;
        }
        
        /// <summary>
        /// Run ingest based on <see cref="IncomingIngestEvent"/>.
        /// </summary>
        /// <returns>Result of ingest operations</returns>
        /// <remarks>This is to comply with message format sent by Deliverator API.</remarks>
        public Task<IngestResult> Ingest(IncomingIngestEvent request, CancellationToken cancellationToken)
        {
            try
            {
                var internalIngestRequest = request.ConvertToInternalRequest();
                return Ingest(internalIngestRequest, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception ingesting IncomingIngest", request.Message);
                return Task.FromResult(IngestResult.Failed);
            }
        }

        /// <summary>
        /// Run ingest based on <see cref="IngestAssetRequest"/>.
        /// </summary>
        /// <returns>Result of ingest operations</returns>
        public Task<IngestResult> Ingest(IngestAssetRequest request, CancellationToken cancellationToken)
        {
            // TODO - make Family a char for easier conversion? 
            var ingestor = resolver(request.Asset.Family == "I" ? AssetFamily.Image : AssetFamily.Timebased);

             return ingestor.Ingest(request, cancellationToken);
        }
    }
}