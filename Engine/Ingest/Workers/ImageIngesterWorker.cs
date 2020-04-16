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

        // TODO - inject a HttpClient for TizerBaseUri
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

        protected override async Task FamilySpecificIngest(IngestionContext ingestionContext)
        {
            var engineSettings = EngineOptionsMonitor.CurrentValue;

            // set Thumbnail and ImageOptimisation policies
            var setAssetPolicies = assetPolicyRepository.HydratePolicies(ingestionContext.Asset, AssetPolicies.All);

            // Put file in correct place for processing 
            CopyAssetFromProcessingToTemplatedFolder(ingestionContext, engineSettings);

            await setAssetPolicies;

            // Call tizer/appetiser to process images
            await imageProcessor.ProcessImage(ingestionContext);

            // TODO tidy up? Save new image sizes to DB? Check deliverator
        }

        private void CopyAssetFromProcessingToTemplatedFolder(IngestionContext context, EngineSettings engineSettings)
        {
            var template = engineSettings.ImageIngest.SourceTemplate;
            var root = engineSettings.ScratchRoot;
            var sourcePath = context.AssetFromOrigin.LocationOnDisk;
            var targetPath = string.Empty;

            try
            {
                var extension = GetFileExtension(context);

                targetPath = TemplatedFolders.GenerateTemplate(template, root, context.Asset);
                
                // HACK - this is to get it working nice locally as appetiser/tizer root needs to be unix + relative to it
                var unixRoot = string.IsNullOrEmpty(engineSettings.ImageProcessorRoot)
                    ? engineSettings.ScratchRoot
                    : engineSettings.ImageProcessorRoot; 
                var unixPath = TemplatedFolders.GenerateTemplateForUnix(template, unixRoot, context.Asset);

                Directory.CreateDirectory(targetPath);
                
                unixPath += $".{extension}";
                targetPath += $".{extension}";

                File.Copy(sourcePath, targetPath, true);

                context.AssetFromOrigin.LocationOnDisk = targetPath;
                context.AssetFromOrigin.RelativeLocationOnDisk = unixPath;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error copying image {assetId} from {sourcePath} to {targetPath}", context.Asset.Id,
                    sourcePath, targetPath);
                throw new ApplicationException($"Error copying image asset from {sourcePath} to {targetPath}.", ex);
            }
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
    }
}