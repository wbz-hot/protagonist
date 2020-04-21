using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Storage;
using DLCS.Repository.Assets;
using DLCS.Repository.Storage;
using DLCS.Web.Requests;
using Engine.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Engine.Ingest.Image
{
    public class ImageProcessor
    {
        private readonly HttpClient httpClient;
        private readonly IOptionsMonitor<EngineSettings> engineOptionsMonitor;
        private readonly ILogger<ImageProcessor> logger;
        private readonly IAssetRepository assetRepository;
        private readonly IBucketReader bucketReader;
        private readonly IThumbLayoutManager thumbLayoutManager;

        public ImageProcessor(
            HttpClient httpClient, 
            IBucketReader bucketReader,
            IThumbLayoutManager thumbLayoutManager,
            IOptionsMonitor<EngineSettings> engineOptionsMonitor,
            ILogger<ImageProcessor> logger,
            IAssetRepository assetRepository)
        {
            this.httpClient = httpClient;
            this.bucketReader = bucketReader;
            this.thumbLayoutManager = thumbLayoutManager;
            this.engineOptionsMonitor = engineOptionsMonitor;
            this.logger = logger;
            this.assetRepository = assetRepository;
        }

        public async Task<bool> ProcessImage(IngestionContext context)
        {
            ImageLocation imageLocation = null;
            ImageStorage imageStorage = null;
            var errorProcessing = false;
            try
            {
                var responseModel = await CallImageProcessor(context);
                (imageLocation, imageStorage) = await ProcessResponse(context, responseModel);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing image {asset}", context.Asset.Id);
                context.Asset.Error = e.Message;
                errorProcessing = true;
            }

            try
            {
                context.Asset.MarkAsIngestComplete();
                var success = await assetRepository.UpdateIngestedAsset(context.Asset, imageLocation, imageStorage);
                return !errorProcessing && success;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error updating image {asset}", context.Asset.Id);
                return false;
            }
        }
        
        private async Task<ImageProcessorResponseModel> CallImageProcessor(IngestionContext context)
        {
            // call tizer/appetiser
            var requestModel = CreateModel(context, engineOptionsMonitor.CurrentValue);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, (Uri)null);
            request.SetJsonContent(requestModel);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // TODO - can get a 200 when appetiser doesn't do anything, e.g. body not understood
            var responseModel = await response.Content.ReadAsAsync<ImageProcessorResponseModel>();
            return responseModel;
        }
        
        private ImageProcessorRequestModel CreateModel(IngestionContext context, EngineSettings engineSettings)
        {
            var asset = context.Asset;
            var imageOptimisationPolicy = asset.FullImageOptimisationPolicy;
            if (imageOptimisationPolicy.TechnicalDetails.Count > 1)
            {
                logger.LogWarning(
                    "ImageOptimisationPolicy {policyId} has {techDetailsCount} technicalDetails but we can only provide 1",
                    imageOptimisationPolicy.Id, imageOptimisationPolicy.TechnicalDetails.Count);
            }

            // HACK - this is to get it working nice locally as appetiser/tizer root needs to be unix + relative to it
            var root = string.IsNullOrEmpty(engineSettings.ImageProcessorRoot)
                ? engineSettings.ScratchRoot
                : engineSettings.ImageProcessorRoot;

            var destFolder =
                TemplatedFolders.GenerateTemplateForUnix(engineSettings.ImageIngest.DestinationTemplate, root, asset);
            var requestModel = new ImageProcessorRequestModel
            {
                Destination = $"{destFolder}/{asset.GetUniqueName()}.jp2",
                Operation = "ingest", // TODO - this may be derivatives-only
                Optimisation = imageOptimisationPolicy.TechnicalDetails.FirstOrDefault(),
                Origin = asset.Origin,
                Source = context.AssetFromOrigin.RelativeLocationOnDisk,
                ImageId = asset.GetUniqueName(),
                JobId = Guid.NewGuid().ToString(),
                ThumbDir = TemplatedFolders.GenerateTemplateForUnix(engineSettings.ImageIngest.ThumbsTemplate,
                    root, asset),
                ThumbSizes = asset.FullThumbnailPolicy.Sizes
            };

            return requestModel;
        }

        private async Task<(ImageLocation imageLocation, ImageStorage imageStorage)> ProcessResponse(
            IngestionContext context, ImageProcessorResponseModel responseModel)
        {
            UpdateImageSize(context.Asset, responseModel);

            var imageLocation = await ProcessOriginImage(context);

            await CreateNewThumbs(context, responseModel);

            var imageStorage = GetImageStorage(context, responseModel);

            return (imageLocation, imageStorage);
            /* TODO
               - Update Batch - IncrementCompleted and IncrementErrors. Probably at a level higher up than this.
             */
        }

        private void UpdateImageSize(Asset asset, ImageProcessorResponseModel responseModel)
        {
            asset.Height = responseModel.Height;
            asset.Width = responseModel.Width;
        }

        private async Task<ImageLocation> ProcessOriginImage(IngestionContext context)
        {
            var jp2Object = new ObjectInBucket(
                engineOptionsMonitor.CurrentValue.Thumbs.StorageBucket,
                context.Asset.GetStorageKey());

            var engineSettings = engineOptionsMonitor.CurrentValue;
            var asset = context.Asset;

            var imageLocation = new ImageLocation {Id = asset.Id};

            var originStrategy = context.AssetFromOrigin.CustomerOriginStrategy;
            if (originStrategy.Optimised && originStrategy.Strategy == OriginStrategy.S3Ambient)
            {
                // Optimised strategy - we don't want to store, just set imageLocation
                var regionalisedBucket = RegionalisedObjectInBucket.Parse(asset.Origin);
                if (string.IsNullOrEmpty(regionalisedBucket.Region))
                {
                    regionalisedBucket.Region = bucketReader.DefaultRegion;
                }

                imageLocation.S3 = regionalisedBucket.GetS3QualifiedUri();
                return imageLocation;
            }

            if (originStrategy.Optimised)
            {
                logger.LogWarning("Asset {id} has originStrategy '{originStrategy}', which is optimised but not S3",
                    asset.Id, originStrategy.Id);
            }

            // Not optimised - upload JP2 to S3 and set ImageLocation to new bucket location
            if (!await bucketReader.WriteFileToBucket(jp2Object, context.AssetFromOrigin.LocationOnDisk))
            {
                // TODO - exception type
                throw new ApplicationException("Failed to write jp2 to storage bucket");
            }

            imageLocation.S3 = string.Format(engineSettings.S3Template, asset.Customer, asset.Space,
                asset.GetUniqueName());
            return imageLocation;
        }

        private async Task CreateNewThumbs(IngestionContext context, ImageProcessorResponseModel responseModel)
        {
            var rootObject = new ObjectInBucket(
                engineOptionsMonitor.CurrentValue.Thumbs.ThumbsBucket,
                $"{context.Asset.GetStorageKey()}/");
            
            SetThumbsOnDiskLocation(context, responseModel);

            await thumbLayoutManager.CreateNewThumbs(context.Asset, responseModel.Thumbs, rootObject);
        }

        private void SetThumbsOnDiskLocation(IngestionContext context, ImageProcessorResponseModel responseModel)
        {
            // Update the location of all thumbs to be full path on disk.
            var settings = engineOptionsMonitor.CurrentValue;
            var partialTemplate = TemplatedFolders.GenerateTemplate(settings.ImageIngest.ThumbsTemplate,
                settings.ScratchRoot, context.Asset);
            foreach (var thumb in responseModel.Thumbs)
            {
                var key = thumb.Path.Substring(thumb.Path.LastIndexOf('/') + 1);
                thumb.Path = string.Concat(partialTemplate, key);
            }
        }
        
        private ImageStorage GetImageStorage(IngestionContext context, ImageProcessorResponseModel responseModel)
        { 
            var asset = context.Asset;

            var thumbSizes = responseModel.Thumbs.Sum(t => GetFileSize(t.Path));
            
            return new ImageStorage
            {
                Id = asset.Id,
                Customer = asset.Customer,
                Space = asset.Space,
                LastChecked = DateTime.Now, // TODO - this should be DateTime.UtcNow
                Size = context.AssetFromOrigin.AssetSize,
                ThumbnailSize = thumbSizes
            };
        }

        private long GetFileSize(string path)
        {
            try
            {
                var fi = new FileInfo(path);
                return fi.Length;
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex, "Error getting fileSize for {path}", path);
                return 0;
            }
        }
    }
}