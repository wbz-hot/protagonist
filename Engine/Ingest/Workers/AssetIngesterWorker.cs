using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
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
        protected readonly IOptionsMonitor<EngineSettings> OptionsMonitor;

        public AssetIngesterWorker(IAssetFetcher assetFetcher, IOptionsMonitor<EngineSettings> optionsMonitor)
        {
            this.assetFetcher = assetFetcher;
            this.OptionsMonitor = optionsMonitor;
        }
        
        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CancellationToken cancellationToken)
        {
            var engineSettings = OptionsMonitor.CurrentValue;
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

        protected abstract Task FamilySpecificIngest(IngestionContext ingestionContext);

        protected class IngestionContext
        {
            public Asset Asset { get; }
            
            public AssetFromOrigin AssetFromOrigin { get; }
            
            public IngestionContext(Asset asset, AssetFromOrigin assetFromOrigin)
            {
                Asset = asset;
                AssetFromOrigin = assetFromOrigin;
            }
        }
    }
}