using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Storage;
using DLCS.Repository.Assets;
using DLCS.Repository.Storage;
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
        private readonly IConfiguration configuration;
        private readonly IBucketReader bucketReader;
        private readonly IThumbLayoutManager thumbLayoutManager;

        public ImageProcessor(
            HttpClient httpClient, 
            IBucketReader bucketReader,
            IThumbLayoutManager thumbLayoutManager,
            IOptionsMonitor<EngineSettings> engineOptionsMonitor,
            ILogger<ImageProcessor> logger,
            IConfiguration configuration)
        {
            this.httpClient = httpClient;
            this.bucketReader = bucketReader;
            this.thumbLayoutManager = thumbLayoutManager;
            this.engineOptionsMonitor = engineOptionsMonitor;
            this.logger = logger;
            this.configuration = configuration;
        }

        public async Task ProcessImage(IngestionContext context)
        {
            var responseModel = await CallImageProcessor(context);
            await ProcessResponse(context, responseModel);
        }
        
        private async Task<ImageProcessorResponseModel> CallImageProcessor(IngestionContext context)
        {
            // call tizer/appetiser
            var requestModel = CreateModel(context, engineOptionsMonitor.CurrentValue);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, (Uri)null);
            
            var serializer = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            
            using var requestBody = new StringContent(JsonConvert.SerializeObject(requestModel, serializer), Encoding.UTF8,
                "application/json");
            request.Content = requestBody;

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

        private async Task ProcessResponse(IngestionContext context, ImageProcessorResponseModel responseModel)
        {
            UpdateImageSize(context.Asset, responseModel);

            var rootObject = new ObjectInBucket(
                engineOptionsMonitor.CurrentValue.Thumbs.StorageBucket,
                context.Asset.GetStorageKey());

            var imageLocation = await GetImageLocation(context, rootObject);
            
            await CreateNewThumbs(context, responseModel, rootObject);

            /* TODO
               - Save Image + ImageLocation records 
               - create thumbs (new + legacy). Which we should have some of for thumbRearranger.
               - create info.json
             */
        }

        private void UpdateImageSize(Asset asset, ImageProcessorResponseModel responseModel)
        {
            asset.Height = responseModel.Height;
            asset.Width = responseModel.Width;
        }

        private async Task<ImageLocation> GetImageLocation(IngestionContext context, ObjectInBucket rootObject)
        {
            var engineSettings = engineOptionsMonitor.CurrentValue;
            var asset = context.Asset;
            var baseBucket = asset.GetStorageKey();
            
            var imageLocation = new ImageLocation {Id = asset.Id};
            if (!context.AssetFromOrigin.CustomerOriginStrategy.Optimised)
            {
                // Not optimised - upload JP2 to S3 and set ImageLocation to new bucket location
                var jp2Object = rootObject.CloneWithKey($"{baseBucket}/{asset.GetUniqueName()}.jp2"); 

                if (!await bucketReader.WriteFileToBucket(jp2Object, context.AssetFromOrigin.LocationOnDisk))
                {
                    // TODO - exception type
                    throw new ApplicationException("Failed to write jp2 to storage bucket");
                }

                imageLocation.S3 = string.Format(engineSettings.S3Template, asset.Customer, asset.Space,
                    asset.GetUniqueName());
            }
            else
            {
                // Optimised strategy - we don't want to store, just set imageLocation
                var regionalisedBucket = RegionalisedObjectInBucket.Parse(asset.Origin);
                if (string.IsNullOrEmpty(regionalisedBucket.Region))
                {
                    regionalisedBucket.Region = configuration["AWS:Region"];
                }

                imageLocation.S3 = regionalisedBucket.GetS3QualifiedUri();
            }

            return imageLocation;
        }
        
        private async Task CreateNewThumbs(IngestionContext context, ImageProcessorResponseModel responseModel,
            ObjectInBucket rootObject)
        {
            SetThumbsOnDiskLocation(context, responseModel);

            await thumbLayoutManager.CreateNewThumbs(context.Asset, responseModel.Thumbs, rootObject);
        }

        private void SetThumbsOnDiskLocation(IngestionContext context, ImageProcessorResponseModel responseModel)
        {
            // Update the location of all thumbs to be full path
            var settings = engineOptionsMonitor.CurrentValue;
            var partialTemplate = TemplatedFolders.GenerateTemplate(settings.ImageIngest.ThumbsTemplate,
                settings.ScratchRoot, context.Asset, false);
            foreach (var thumb in responseModel.Thumbs)
            {
                var key = thumb.Path.Substring(thumb.Path.LastIndexOf('/') + 1);
                thumb.Path = partialTemplate.Replace(TemplatedFolders.Image, key);
            }
        }
    }
}