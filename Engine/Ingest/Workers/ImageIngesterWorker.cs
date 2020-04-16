using System;
using System.IO;
using System.Threading.Tasks;
using DLCS.Core;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    public class ImageIngesterWorker : AssetIngesterWorker
    {
        private readonly ILogger<ImageIngesterWorker> logger;

        // TODO - inject a HttpClient for TizerBaseUri
        public ImageIngesterWorker(
            IAssetFetcher assetFetcher, 
            IOptionsMonitor<EngineSettings> optionsMonitor,
            ILogger<ImageIngesterWorker> logger) 
            : base(assetFetcher, optionsMonitor)
        {
            this.logger = logger;
        }

        protected override Task FamilySpecificIngest(IngestionContext ingestionContext)
        {
            var engineSettings = OptionsMonitor.CurrentValue;

            // Put file in correct place for processing 
            CopyAssetFromProcessingToTemplatedFolder(ingestionContext, engineSettings);
            
            /* TODO
             - move files around
             - get thumbnailPolicy
             - get imageOptimisationPolicy
             - call tizer
             - handle tizer response
             */
            
            
            return Task.CompletedTask;
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
                targetPath += $".{extension}";
                
                File.Copy(sourcePath, targetPath, true);
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