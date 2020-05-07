using System;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using Engine.Ingest.Models;
using Engine.Ingest.Timebased;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    public class TimebasedIngesterWorker : IAssetIngesterWorker
    {
        private readonly IAssetMover<AssetInBucket> assetMover;
        private readonly IMediaTranscoder mediaTranscoder;
        private readonly EngineSettings engineSettings;
        private readonly ILogger<TimebasedIngesterWorker> logger;

        public TimebasedIngesterWorker(
            IAssetMover<AssetInBucket> assetMover,
            IOptionsMonitor<EngineSettings> engineOptions,
            IMediaTranscoder mediaTranscoder,
            ILogger<TimebasedIngesterWorker> logger)
        {
            this.assetMover = assetMover;
            this.mediaTranscoder = mediaTranscoder;
            engineSettings = engineOptions.CurrentValue;
            this.logger = logger;
        }

        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CustomerOriginStrategy customerOriginStrategy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var assetInBucket = await assetMover.CopyAsset(
                    ingestAssetRequest.Asset,
                    engineSettings.TimebasedIngest.S3InputTemplate,
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

                var success = await mediaTranscoder.InitiateTranscodeOperation(context, cancellationToken);
                return success ? IngestResult.QueuedForProcessing : IngestResult.Failed;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ingesting image {assetId}", ingestAssetRequest.Asset.Id);
                return IngestResult.Failed;
            }
        }

        private bool SkipStoragePolicyCheck(int customerId)
        {
            var customerSpecific = engineSettings.GetCustomerSettings(customerId);
            return customerSpecific.NoStoragePolicyCheck;
        }
    }
}