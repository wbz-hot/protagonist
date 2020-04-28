using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DLCS.Core;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Storage;
using DLCS.Repository.Assets;
using DLCS.Repository.Storage;
using DLCS.Web.Requests;
using Engine.Ingest.Workers;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Image
{
    public class ImageProcessor : IImageProcessor
    {
        private readonly HttpClient httpClient;
        private readonly EngineSettings engineSettings;
        private readonly ILogger<ImageProcessor> logger;
        private readonly IBucketReader bucketReader;
        private readonly IThumbLayoutManager thumbLayoutManager;

        public ImageProcessor(
            HttpClient httpClient,
            IBucketReader bucketReader,
            IThumbLayoutManager thumbLayoutManager,
            IOptionsMonitor<EngineSettings> engineOptionsMonitor,
            ILogger<ImageProcessor> logger)
        {
            this.httpClient = httpClient;
            this.bucketReader = bucketReader;
            this.thumbLayoutManager = thumbLayoutManager;
            this.engineSettings = engineOptionsMonitor.CurrentValue;
            this.logger = logger;
        }

        public async Task<bool> ProcessImage(IngestionContext context)
        {
            try
            {
                var derivativesOnly = DerivativesOnly(context.AssetFromOrigin);
                var responseModel = await CallImageProcessor(context, derivativesOnly);
                var (imageLocation, imageStorage) = await ProcessResponse(context, responseModel, derivativesOnly);
                context.WithLocation(imageLocation).WithStorage(imageStorage);
                return true;    
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing image {asset}", context.Asset.Id);
                context.Asset.Error = e.Message;
                return false;
            }
        }

        private async Task<ImageProcessorResponseModel> CallImageProcessor(IngestionContext context,
            bool derivativesOnly)
        {
            // call tizer/appetiser
            var requestModel = CreateModel(context, derivativesOnly);

            using var request = new HttpRequestMessage(HttpMethod.Post, (Uri) null);
            request.SetJsonContent(requestModel);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // TODO - can get a 200 when appetiser doesn't do anything, e.g. body not understood
            var responseModel = await response.Content.ReadAsAsync<ImageProcessorResponseModel>();
            return responseModel;
        }

        private ImageProcessorRequestModel CreateModel(IngestionContext context, bool derivativesOnly)
        {
            var asset = context.Asset;
            var imageOptimisationPolicy = asset.FullImageOptimisationPolicy;
            if (imageOptimisationPolicy.TechnicalDetails.Count > 1)
            {
                logger.LogWarning(
                    "ImageOptimisationPolicy {policyId} has {techDetailsCount} technicalDetails but we can only provide 1",
                    imageOptimisationPolicy.Id, imageOptimisationPolicy.TechnicalDetails.Count);
            }
            
            var requestModel = new ImageProcessorRequestModel
            {
                Destination = GetJP2File(asset, true),
                Operation = derivativesOnly ? "derivatives-only" : "ingest",
                Optimisation = imageOptimisationPolicy.TechnicalDetails.FirstOrDefault(),
                Origin = asset.Origin,
                Source = context.AssetFromOrigin.RelativeLocationOnDisk,
                ImageId = asset.GetUniqueName(),
                JobId = Guid.NewGuid().ToString(),
                ThumbDir = TemplatedFolders.GenerateTemplateForUnix(engineSettings.ImageIngest.ThumbsTemplate,
                    engineSettings.GetRoot(true), asset),
                ThumbSizes = asset.FullThumbnailPolicy.Sizes
            };

            return requestModel;
        }

        private string GetJP2File(Asset asset, bool forImageProcessor)
        {
            // Appetiser/Tizer want unix paths relative to mount share.
            // This logic allows handling when running locally on win/unix and when deployed to unix
            var destFolder = forImageProcessor
                ? TemplatedFolders.GenerateTemplateForUnix(engineSettings.ImageIngest.DestinationTemplate,
                    engineSettings.GetRoot(true), asset)
                : TemplatedFolders.GenerateTemplate(engineSettings.ImageIngest.DestinationTemplate,
                    engineSettings.GetRoot(), asset);

            return $"{destFolder}{asset.GetUniqueName()}.jp2";
        }

        private async Task<(ImageLocation imageLocation, ImageStorage imageStorage)> ProcessResponse(
            IngestionContext context, ImageProcessorResponseModel responseModel, bool derivativesOnly)
        {
            UpdateImageSize(context.Asset, responseModel);

            var imageLocation = await ProcessOriginImage(context, derivativesOnly);

            await CreateNewThumbs(context, responseModel);

            var imageStorage = GetImageStorage(context, responseModel);

            return (imageLocation, imageStorage);
        }

        private void UpdateImageSize(Asset asset, ImageProcessorResponseModel responseModel)
        {
            asset.Height = responseModel.Height;
            asset.Width = responseModel.Width;
        }

        private async Task<ImageLocation> ProcessOriginImage(IngestionContext context, bool derivativesOnly)
        {
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

            var jp2BucketObject = new ObjectInBucket(
                engineSettings.Thumbs.StorageBucket,
                context.Asset.GetStorageKey());

            // if derivatives-only, no new JP2 will have been generated so use the 'origin' file
            var jp2File = derivativesOnly ? context.AssetFromOrigin.LocationOnDisk : GetJP2File(context.Asset, false);
            
            // Not optimised - upload JP2 to S3 and set ImageLocation to new bucket location
            if (!await bucketReader.WriteFileToBucket(jp2BucketObject, jp2File))
            {
                throw new ApplicationException($"Failed to write jp2 {jp2File} to storage bucket");
            }

            imageLocation.S3 = string.Format(engineSettings.S3Template, asset.Customer, asset.Space,
                asset.GetUniqueName());
            return imageLocation;
        }

        private async Task CreateNewThumbs(IngestionContext context, ImageProcessorResponseModel responseModel)
        {
            var rootObject = new ObjectInBucket(
                engineSettings.Thumbs.ThumbsBucket,
                $"{context.Asset.GetStorageKey()}/");

            SetThumbsOnDiskLocation(context, responseModel);

            await thumbLayoutManager.CreateNewThumbs(context.Asset, responseModel.Thumbs, rootObject);
        }

        private void SetThumbsOnDiskLocation(IngestionContext context, ImageProcessorResponseModel responseModel)
        {
            // Update the location of all thumbs to be full path on disk.
            var partialTemplate = TemplatedFolders.GenerateTemplate(engineSettings.ImageIngest.ThumbsTemplate,
                engineSettings.GetRoot(), context.Asset);
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

        private bool DerivativesOnly(AssetFromOrigin assetFromOrigin)
            => assetFromOrigin.ContentType == MIMEHelper.JP2 || assetFromOrigin.ContentType == MIMEHelper.JPX;
    }
}