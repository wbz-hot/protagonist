using System;
using System.IO;
using System.Threading.Tasks;
using DLCS.Core;
using DLCS.Model.Assets;
using Engine.Ingest.Image;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    public class ImageIngesterWorker : AssetIngesterWorker
    {
        private readonly ImageProcessor imageProcessor;
        private readonly IAssetPolicyRepository assetPolicyRepository;
        private readonly ILogger<ImageIngesterWorker> logger;

        public ImageIngesterWorker(
            ImageProcessor imageProcessor,
            IAssetFetcher assetFetcher,
            IAssetPolicyRepository assetPolicyRepository,
            IOptionsMonitor<EngineSettings> engineOptionsMonitor,
            ILogger<ImageIngesterWorker> logger)
            : base(assetFetcher, engineOptionsMonitor)
        {
            this.imageProcessor = imageProcessor;
            this.assetPolicyRepository = assetPolicyRepository;
            this.logger = logger;
        }

        protected override async Task<IngestResult> FamilySpecificIngest(IngestionContext ingestionContext)
        {
            // set Thumbnail and ImageOptimisation policies
            var setAssetPolicies = assetPolicyRepository.HydratePolicies(ingestionContext.Asset, AssetPolicies.All);

            // Put file in correct place for processing 
            var sourceDir = CopyAssetFromProcessingToTemplatedFolder(ingestionContext, EngineOptionsMonitor.CurrentValue);

            await setAssetPolicies;

            // Call tizer/appetiser to process images - create thumbs, update DB
            var processSuccess = await imageProcessor.ProcessImage(ingestionContext);

            // Processing has occurred, clear down the root folder used for processing
            CleanupWorkingFolder(sourceDir);

            // TODO - handle calling Orchestrator if customer specific value (or override) set.

            return processSuccess ? IngestResult.Success : IngestResult.Failed;
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

                // HACK - this is to get it working nice locally as appetiser/tizer root needs to be unix + relative
                var unixRoot = string.IsNullOrEmpty(engineSettings.ImageProcessorRoot)
                    ? engineSettings.ScratchRoot
                    : engineSettings.ImageProcessorRoot;
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
            var root = engineSettings.ScratchRoot;
            var imageIngest = engineSettings.ImageIngest;
            
            // source is the main folder for storing
            var source = TemplatedFolders.GenerateTemplate(imageIngest.SourceTemplate, root, context.Asset);
            
            // dest is the folder where image-processor will copy output
            var dest = TemplatedFolders.GenerateTemplate(imageIngest.DestinationTemplate, root, context.Asset);
            
            // thumb is the folder where generated thumbnails will be output
            var thumb = TemplatedFolders.GenerateTemplate(imageIngest.ThumbsTemplate, engineSettings.ScratchRoot,
                context.Asset);

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
        
        private void CleanupWorkingFolder(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return;

            try
            {
                Directory.Delete(rootPath, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to delete directory {rootPath}", rootPath);
            }
        }
    }
}