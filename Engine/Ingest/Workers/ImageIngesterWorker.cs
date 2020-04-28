using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Policies;
using Engine.Ingest.Completion;
using Engine.Ingest.Image;
using Engine.Ingest.Models;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    public class ImageIngesterWorker : IAssetIngesterWorker
    {
        private readonly IAssetFetcher assetFetcher;
        private readonly EngineSettings engineSettings;
        private readonly IImageProcessor imageProcessor;
        private readonly IPolicyRepository policyRepository;
        private readonly IIngestorCompletion imageCompletion;
        private readonly ILogger<ImageIngesterWorker> logger;

        public ImageIngesterWorker(
            IImageProcessor imageProcessor,
            IAssetFetcher assetFetcher,
            IPolicyRepository policyRepository,
            IOptionsMonitor<EngineSettings> engineOptions,
            IIngestorCompletion imageCompletion,
            ILogger<ImageIngesterWorker> logger)
        {
            this.assetFetcher = assetFetcher;
            engineSettings = engineOptions.CurrentValue;
            this.imageProcessor = imageProcessor;
            this.policyRepository = policyRepository;
            this.imageCompletion = imageCompletion;
            this.logger = logger;
        }
        
        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sourceTemplate = GetSourceTemplate(ingestAssetRequest.Asset);

                var fetchedAsset = await assetFetcher.CopyAssetToDisk(
                    ingestAssetRequest.Asset, 
                    sourceTemplate, 
                    !SkipStoragePolicyCheck(ingestAssetRequest.Asset.Customer),
                    cancellationToken);

                var context = new IngestionContext(ingestAssetRequest.Asset, fetchedAsset);
                if (fetchedAsset.FileExceedsAllowance)
                {
                    ingestAssetRequest.Asset.Error = "StoragePolicy size limit exceeded";
                    await imageCompletion.CompleteIngestion(context, false, sourceTemplate);
                }

                var ingestSuccess = await DoIngest(context);

                var completionSuccess = await imageCompletion.CompleteIngestion(context, ingestSuccess, sourceTemplate);

                return ingestSuccess && completionSuccess ? IngestResult.Success : IngestResult.Failed;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ingesting image {assetId}", ingestAssetRequest.Asset.Id);
                return IngestResult.Failed;
            }
        }

        private async Task<bool> DoIngest(IngestionContext ingestionContext)
        {
            // set Thumbnail and ImageOptimisation policies
            var setAssetPolicies = policyRepository.HydrateAssetPolicies(ingestionContext.Asset, AssetPolicies.All);

            // Put file in correct place for processing 
            SetRelativeLocationOnDisk(ingestionContext);

            await setAssetPolicies;

            // Call tizer/appetiser to process images - create thumbs, update DB
            var processSuccess = await imageProcessor.ProcessImage(ingestionContext);

            return processSuccess;
        }

        private void SetRelativeLocationOnDisk(IngestionContext context)
        {
            var assetOnDisk = context.AssetFromOrigin.LocationOnDisk;
            var extension = assetOnDisk.Substring(assetOnDisk.LastIndexOf(".", StringComparison.Ordinal) + 1);

            // this is to get it working nice locally as appetiser/tizer root needs to be unix + relative to it
            var unixRoot = engineSettings.GetRoot(true);
            var unixPath = TemplatedFolders.GenerateTemplateForUnix(engineSettings.ImageIngest.SourceTemplate,
                unixRoot, context.Asset);

            unixPath += $".{extension}";

            context.AssetFromOrigin.RelativeLocationOnDisk = unixPath;
        }

        private string GetSourceTemplate(Asset asset)
        {
            var root = engineSettings.GetRoot();
            var imageIngest = engineSettings.ImageIngest;
            
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
