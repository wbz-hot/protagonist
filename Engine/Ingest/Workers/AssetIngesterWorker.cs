using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Models;
using Engine.Settings;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    /// <summary>
    /// Base class for ingesting assets.
    /// </summary>
    public abstract class AssetIngesterWorker : IAssetIngesterWorker
    {
        private readonly IAssetFetcher assetFetcher;
        protected readonly IOptionsMonitor<EngineSettings> EngineOptionsMonitor;

        public AssetIngesterWorker(IAssetFetcher assetFetcher, IOptionsMonitor<EngineSettings> engineOptionsMonitor)
        {
            this.assetFetcher = assetFetcher;
            this.EngineOptionsMonitor = engineOptionsMonitor;
        }
        
        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CancellationToken cancellationToken)
        {
            var engineSettings = EngineOptionsMonitor.CurrentValue;
            var fetchedAsset = await assetFetcher.CopyAssetFromOrigin(ingestAssetRequest.Asset,
                engineSettings.ProcessingFolder,
                cancellationToken);
            
            // TODO - create and update ImageLocation record
            // TODO - CheckStoragePolicy. Checks if there is enough space to store this 

            // call image or ElasticTranscoder
            var context = new IngestionContext(ingestAssetRequest.Asset, fetchedAsset);
            await FamilySpecificIngest(context);
            
            // update batch
            // set response (if image)

            return IngestResult.Success;
        }

        // TODO - return some sort of response code/bool to signify if complete?
        protected abstract Task FamilySpecificIngest(IngestionContext ingestionContext);
    }
}