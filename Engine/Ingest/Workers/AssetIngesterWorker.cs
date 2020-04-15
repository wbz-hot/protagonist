using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Models;
using Engine.Settings;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    /// <summary>
    /// Base class for ingestions.
    /// </summary>
    public abstract class AssetIngesterWorker : IAssetIngesterWorker
    {
        private readonly IAssetFetcher assetFetcher;
        private readonly IOptionsMonitor<EngineSettings> optionsMonitor;

        public AssetIngesterWorker(IAssetFetcher assetFetcher, IOptionsMonitor<EngineSettings> optionsMonitor)
        {
            this.assetFetcher = assetFetcher;
            this.optionsMonitor = optionsMonitor;
        }
        
        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CancellationToken cancellationToken)
        {
            var engineSettings = optionsMonitor.CurrentValue;
            await assetFetcher.CopyAssetFromOrigin(ingestAssetRequest.Asset, engineSettings.ProcessingFolder,
                cancellationToken);
            
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