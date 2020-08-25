using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IAssetMover assetMover;
        private readonly IMediaTranscoder mediaTranscoder;
        private readonly EngineSettings engineSettings;
        private readonly ILogger<TimebasedIngesterWorker> logger;
        private static readonly Random random = new Random(); 

        public TimebasedIngesterWorker(
            AssetMoverResolver assetMoverResolver,
            IOptionsMonitor<EngineSettings> engineOptions,
            IMediaTranscoder mediaTranscoder,
            ILogger<TimebasedIngesterWorker> logger)
        {
            assetMover = assetMoverResolver(AssetMoveType.ObjectStore);
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
                var stopwatch = Stopwatch.StartNew();
                
                var targetFolder = $"{engineSettings.TimebasedIngest.S3InputTemplate}{GetRandomPrefix()}/";
                
                var assetInBucket = await assetMover.CopyAsset(
                    ingestAssetRequest.Asset,
                    targetFolder,
                    !SkipStoragePolicyCheck(ingestAssetRequest.Asset.Customer),
                    customerOriginStrategy,
                    cancellationToken);
                stopwatch.Stop();
                logger.LogInformation("Copied timebased asset in {elapsed}ms", stopwatch.ElapsedMilliseconds,
                    ingestAssetRequest.Asset.Id);
                
                var context = new IngestionContext(ingestAssetRequest.Asset, assetInBucket);
                if (assetInBucket.FileExceedsAllowance)
                {
                    ingestAssetRequest.Asset.Error = "StoragePolicy size limit exceeded";
                    // TODO await imageCompletion.CompleteIngestion(context, false, sourceTemplate);
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
        
        private string GetRandomPrefix() => random.Next(0, 9999).ToString("D4");
    }
}