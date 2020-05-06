using System;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using Engine.Ingest.Models;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    public class TimebasedIngesterWorker : IAssetIngesterWorker
    {
        private readonly IAssetMover<AssetInBucket> assetMover;
        private readonly EngineSettings engineSettings;
        private readonly ILogger<TimebasedIngesterWorker> logger;

        public TimebasedIngesterWorker(
            IAssetMover<AssetInBucket> assetMover,
            IOptionsMonitor<EngineSettings> engineOptions,
            ILogger<TimebasedIngesterWorker> logger)
        {
            this.assetMover = assetMover;
            engineSettings = engineOptions.CurrentValue;
            this.logger = logger;
        }

        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CustomerOriginStrategy customerOriginStrategy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if the derivatives exist (based on customer overrides)

                var sourceTemplate = GetBucketTarget(ingestAssetRequest.Asset);
                var assetInBucket = await assetMover.CopyAsset(
                    ingestAssetRequest.Asset,
                    sourceTemplate,
                    !SkipStoragePolicyCheck(ingestAssetRequest.Asset.Customer),
                    customerOriginStrategy,
                    cancellationToken);
                
                var context = new IngestionContext(ingestAssetRequest.Asset, assetInBucket);
                if (assetInBucket.FileExceedsAllowance)
                {
                    ingestAssetRequest.Asset.Error = "StoragePolicy size limit exceeded";
                    // await imageCompletion.CompleteIngestion(context, false, sourceTemplate);
                    return IngestResult.Failed;
                }
                /*
                 * create a new ElasticTranscoder job IF derivatives don't exist
                 */
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ingesting image {assetId}", ingestAssetRequest.Asset.Id);
                return IngestResult.Failed;
            }

            throw new System.NotImplementedException();
        }

        private string GetBucketTarget(Asset asset)
        {
            throw new NotImplementedException();
        }

        private bool SkipStoragePolicyCheck(int customerId)
        {
            var customerSpecific = engineSettings.GetCustomerSettings(customerId);
            return customerSpecific.NoStoragePolicyCheck;
        }
    }
}