using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Models;

namespace Engine.Ingest.Workers
{
    /// <summary>
    /// Base class for ingestions.
    /// </summary>
    public abstract class AssetIngesterWorker : IAssetIngesterWorker
    {
        private readonly IAssetFetcher assetFetcher;

        public AssetIngesterWorker(IAssetFetcher assetFetcher)
        {
            this.assetFetcher = assetFetcher;
        }
        
        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CancellationToken cancellationToken)
        {
            // TODO - get folder from config
            await assetFetcher.CopyAssetFromOrigin(ingestAssetRequest.Asset, "C:\\temp\\ingest\\", cancellationToken);
            
            // TODO - create and update ImageLocation record
            // TODO - CheckStoragePolicy. Checks if there is enough space to store this 

            // call image or ElasticTranscoder
            await FamilySpecificIngest(ingestAssetRequest);
            
            // update batch
            // set response (if image)

            return IngestResult.Success;
        }

        // TODO - what needs pushed to this method?
        protected abstract Task FamilySpecificIngest(IngestAssetRequest thingToIngestAsset);
    }
}