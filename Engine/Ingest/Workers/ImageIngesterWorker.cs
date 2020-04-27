using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Core;
using DLCS.Model.Assets;
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
        private readonly IAssetPolicyRepository assetPolicyRepository;
        private readonly IIngestorCompletion imageCompletion;
        private readonly ILogger<ImageIngesterWorker> logger;

        public ImageIngesterWorker(
            IImageProcessor imageProcessor,
            IAssetFetcher assetFetcher,
            IAssetPolicyRepository assetPolicyRepository,
            IOptionsMonitor<EngineSettings> engineOptions,
            IIngestorCompletion imageCompletion,
            ILogger<ImageIngesterWorker> logger)
        {
            this.assetFetcher = assetFetcher;
            engineSettings = engineOptions.CurrentValue;
            this.imageProcessor = imageProcessor;
            this.assetPolicyRepository = assetPolicyRepository;
            this.imageCompletion = imageCompletion;
            this.logger = logger;
        }
        
        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sourceTemplate = GetSourceTemplate(ingestAssetRequest.Asset);
                
                var fetchedAsset = await assetFetcher.CopyAssetToDisk(ingestAssetRequest.Asset,
                    sourceTemplate,
                    cancellationToken);

                // TODO - CheckStoragePolicy. Checks if there is enough space to store this 

                var context = new IngestionContext(ingestAssetRequest.Asset, fetchedAsset);
                var ingestSuccess = await DoIngest(context, sourceTemplate);

                var completionSuccess = await imageCompletion.CompleteIngestion(context, ingestSuccess);

                return ingestSuccess && completionSuccess ? IngestResult.Success : IngestResult.Failed;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ingesting image {assetId}", ingestAssetRequest.Asset.Id);
                return IngestResult.Failed;
            }
        }

        private async Task<bool> DoIngest(IngestionContext ingestionContext, string sourceDir)
        {
            // set Thumbnail and ImageOptimisation policies
            var setAssetPolicies = assetPolicyRepository.HydratePolicies(ingestionContext.Asset, AssetPolicies.All);

            // Put file in correct place for processing 
            SetRelativeLocationOnDisk(ingestionContext);

            await setAssetPolicies;

            // Call tizer/appetiser to process images - create thumbs, update DB
            var processSuccess = await imageProcessor.ProcessImage(ingestionContext);

            // Processing has occurred, clear down the root folder used for processing
            CleanupWorkingAssets(sourceDir, ingestionContext.AssetFromOrigin.LocationOnDisk);

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

        private void CleanupWorkingAssets(string rootPath, string locationOnDisk)
        {
            try
            {
                Directory.Delete(rootPath, true);
                File.Delete(locationOnDisk);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cleaning up working assets. {rootPath}, {locationOnDisk}", rootPath, locationOnDisk);
            }
        }
    }
}
