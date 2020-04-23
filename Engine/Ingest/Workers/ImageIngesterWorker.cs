using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Core;
using DLCS.Model.Assets;
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
        private readonly IOptionsMonitor<EngineSettings> engineOptionsMonitor;
        private readonly IAssetRepository assetRepository;
        private readonly ImageProcessor imageProcessor;
        private readonly IAssetPolicyRepository assetPolicyRepository;
        private readonly ILogger<ImageIngesterWorker> logger;

        public ImageIngesterWorker(
            ImageProcessor imageProcessor,
            IAssetFetcher assetFetcher,
            IAssetPolicyRepository assetPolicyRepository,
            IOptionsMonitor<EngineSettings> engineOptionsMonitor,
            IAssetRepository assetRepository,
            ILogger<ImageIngesterWorker> logger)
        {
            this.assetFetcher = assetFetcher;
            this.engineOptionsMonitor = engineOptionsMonitor;
            this.assetRepository = assetRepository;
            this.imageProcessor = imageProcessor;
            this.assetPolicyRepository = assetPolicyRepository;
            this.logger = logger;
        }
        
        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CancellationToken cancellationToken)
        {
            try
            {
                var engineSettings = engineOptionsMonitor.CurrentValue;
                var fetchedAsset = await assetFetcher.CopyAssetFromOrigin(ingestAssetRequest.Asset,
                    engineSettings.ProcessingFolder,
                    cancellationToken);

                // TODO - CheckStoragePolicy. Checks if there is enough space to store this 

                var context = new IngestionContext(ingestAssetRequest.Asset, fetchedAsset);
                var ingestSuccess = await DoIngest(context);

                var markIngestAsComplete = await MarkIngestAsComplete(context);

                // TODO - handle calling Orchestrator if customer specific value (or override) set.

                return ingestSuccess && markIngestAsComplete ? IngestResult.Success : IngestResult.Failed;
            }
            catch (Exception ex)
            {
                return IngestResult.Failed;
            }
        }

        private async Task<bool> MarkIngestAsComplete(IngestionContext context)
        {
            try
            {
                var success =
                    await assetRepository.UpdateIngestedAsset(context.Asset, context.ImageLocation, context.ImageStorage);
                return success;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error updating image {asset}", context.Asset.Id);
                return false;
            }
        }

        private async Task<bool> DoIngest(IngestionContext ingestionContext)
        {
            // set Thumbnail and ImageOptimisation policies
            var setAssetPolicies = assetPolicyRepository.HydratePolicies(ingestionContext.Asset, AssetPolicies.All);

            // Put file in correct place for processing 
            var sourceDir = CopyAssetFromProcessingToTemplatedFolder(ingestionContext, engineOptionsMonitor.CurrentValue);

            await setAssetPolicies;

            // Call tizer/appetiser to process images - create thumbs, update DB
            var processSuccess = await imageProcessor.ProcessImage(ingestionContext);

            // Processing has occurred, clear down the root folder used for processing
            CleanupWorkingAssets(sourceDir, ingestionContext.AssetFromOrigin.LocationOnDisk);

            return processSuccess;
        }

        private string CopyAssetFromProcessingToTemplatedFolder(IngestionContext context, EngineSettings engineSettings)
        {
            var assetOnDisk = context.AssetFromOrigin.LocationOnDisk;
            var targetPath = string.Empty;
            var sourceDir = string.Empty;

            try
            {
                var extension = GetFileExtension(context);
                sourceDir = GetSourceDir(context, engineSettings);

                // this is to get it working nice locally as appetiser/tizer root needs to be unix + relative to it
                var unixRoot = engineSettings.GetRoot(true);
                var unixPath = TemplatedFolders.GenerateTemplateForUnix(engineSettings.ImageIngest.SourceTemplate,
                    unixRoot, context.Asset);

                unixPath += $".{extension}";
                targetPath = $"{sourceDir}.{extension}";

                File.Move(assetOnDisk, targetPath, true);

                context.AssetFromOrigin.LocationOnDisk = targetPath;
                context.AssetFromOrigin.RelativeLocationOnDisk = unixPath;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error copying image {assetId} from {sourcePath} to {targetPath}", context.Asset.Id,
                    assetOnDisk, targetPath);
                throw new ApplicationException($"Error copying image asset from {assetOnDisk} to {targetPath}.", ex);
            }

            return sourceDir;
        }
        
        private string GetSourceDir(IngestionContext context, EngineSettings engineSettings)
        {
            var root = engineSettings.GetRoot();
            var imageIngest = engineSettings.ImageIngest;
            
            // source is the main folder for storing
            var source = TemplatedFolders.GenerateTemplate(imageIngest.SourceTemplate, root, context.Asset);
            
            // dest is the folder where image-processor will copy output
            var dest = TemplatedFolders.GenerateTemplate(imageIngest.DestinationTemplate, root, context.Asset);
            
            // thumb is the folder where generated thumbnails will be output
            var thumb = TemplatedFolders.GenerateTemplate(imageIngest.ThumbsTemplate, root, context.Asset);

            Directory.CreateDirectory(dest);
            Directory.CreateDirectory(thumb);
            Directory.CreateDirectory(source);

            return source;
        }

        private string GetFileExtension(IngestionContext context)
        {
            var extension = MIMEHelper.GetExtensionForContentType(context.AssetFromOrigin.ContentType);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = "file";
                logger.LogWarning("Unable to get a file extension for {contentType}",
                    context.AssetFromOrigin.ContentType, context.Asset.Id);
            }

            return extension;
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