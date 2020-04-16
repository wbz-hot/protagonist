using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DLCS.Model.Storage;
using Engine.Settings;
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
        private readonly IBucketReader bucketReader;

        public ImageProcessor(
            HttpClient httpClient, 
            IBucketReader bucketReader,
            IOptionsMonitor<EngineSettings> engineOptionsMonitor,
            ILogger<ImageProcessor> logger)
        {
            this.httpClient = httpClient;
            this.bucketReader = bucketReader;
            this.engineOptionsMonitor = engineOptionsMonitor;
            this.logger = logger;
        }

        public async Task ProcessImage(IngestionContext context)
        {
            // call tizer/appetiser
            var responseModel = await CallImageProcessor(context);
            await ProcessResponse(responseModel);
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

            // TODO - can get a 200 when appetiser doesn't do anything, e.g. path is dodgy
            var responseModel = await response.Content.ReadAsAsync<ImageProcessorResponseModel>();
            return responseModel;
        }
        
        private ImageProcessorRequestModel CreateModel(IngestionContext context, EngineSettings engineSettings)
        {
            EnsureOutputDirectories(context, engineSettings);
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

        private void EnsureOutputDirectories(IngestionContext context, EngineSettings engineSettings)
        {
            var dest = TemplatedFolders.GenerateTemplateForUnix(engineSettings.ImageIngest.DestinationTemplate,
                engineSettings.ScratchRoot, context.Asset);
            var thumb = TemplatedFolders.GenerateTemplateForUnix(engineSettings.ImageIngest.ThumbsTemplate,
                engineSettings.ScratchRoot, context.Asset);

            Directory.CreateDirectory(dest);
            Directory.CreateDirectory(thumb);
        }

        private Task ProcessResponse(ImageProcessorResponseModel responseModel)
        {
            return Task.CompletedTask;
            /* TODO
             - handle tizer response 
               - create thumbs (new + legacy). Which we should have some of for thumbRearranger.
               - update image size, using dimensions sent back from Tizer?
               - create info.json
             */
        }
    }
}