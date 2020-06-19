using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using Engine.Ingest.Completion;
using Engine.Ingest.Image;
using Engine.Ingest.Models;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    /// <summary>
    /// <see cref="IAssetIngesterWorker"/> for ingesting Image assets (Family = I).
    /// </summary>
    public class ImageIngesterWorker : IAssetIngesterWorker
    {
        private readonly IAssetMover assetMover;
        private readonly EngineSettings engineSettings;
        private readonly IImageProcessor imageProcessor;
        private readonly IIngestorCompletion imageCompletion;
        private readonly ILogger<ImageIngesterWorker> logger;

        public ImageIngesterWorker(
            IImageProcessor imageProcessor,
            AssetMoverResolver assetMoverResolver,
            IOptionsMonitor<EngineSettings> engineOptions,
            IIngestorCompletion imageCompletion,
            ILogger<ImageIngesterWorker> logger)
        {
            assetMover = assetMoverResolver(AssetMoveType.Disk);
            engineSettings = engineOptions.CurrentValue;
            this.imageProcessor = imageProcessor;
            this.imageCompletion = imageCompletion;
            this.logger = logger;
        }
        
        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CustomerOriginStrategy customerOriginStrategy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sourceTemplate = GetSourceTemplate(ingestAssetRequest.Asset);

                var stopwatch = Stopwatch.StartNew(); 
                var assetOnDisk = await assetMover.CopyAsset(
                    ingestAssetRequest.Asset, 
                    sourceTemplate, 
                    !SkipStoragePolicyCheck(ingestAssetRequest.Asset.Customer),
                    customerOriginStrategy,
                    cancellationToken);
                stopwatch.Stop();
                logger.LogInformation("Copied image asset in {elapsed}ms", stopwatch.ElapsedMilliseconds,
                    ingestAssetRequest.Asset.Id);

                var context = new IngestionContext(ingestAssetRequest.Asset, assetOnDisk);
                if (assetOnDisk.FileExceedsAllowance)
                {
                    ingestAssetRequest.Asset.Error = "StoragePolicy size limit exceeded";
                    await imageCompletion.CompleteIngestion(context, false, sourceTemplate);
                    return IngestResult.Failed;
                }

                var ingestSuccess = await imageProcessor.ProcessImage(context);

                var completionSuccess = await imageCompletion.CompleteIngestion(context, ingestSuccess, sourceTemplate);

                return ingestSuccess && completionSuccess ? IngestResult.Success : IngestResult.Failed;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ingesting image {assetId}", ingestAssetRequest.Asset.Id);
                return IngestResult.Failed;
            }
        }

        private string GetSourceTemplate(Asset asset)
        {
            var imageIngest = engineSettings.ImageIngest;
            var root = imageIngest.GetRoot();
            
            // source is the main folder for storing
            var source = TemplatedFolders.GenerateTemplate(imageIngest.SourceTemplate, root, asset);
            
            // dest is the folder where image-processor will copy output
            var dest = TemplatedFolders.GenerateTemplate(imageIngest.DestinationTemplate, root, asset);
            
            // thumb is the folder where generated thumbnails will be output
            var thumb = TemplatedFolders.GenerateTemplate(imageIngest.ThumbsTemplate, root, asset);

            Directory.CreateDirectory(dest);
            Directory.CreateDirectory(thumb);
            Directory.CreateDirectory(source);

            return source;
        }
        
        private bool SkipStoragePolicyCheck(int customerId)
        {
            var customerSpecific = engineSettings.GetCustomerSettings(customerId);
            return customerSpecific.NoStoragePolicyCheck;
        }
    }
}
