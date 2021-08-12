﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Storage;
using DLCS.Repository.Settings;
using DLCS.Repository.Storage;
using IIIF.ImageApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Size = IIIF.Size;

namespace DLCS.Repository.Assets
{
    public class ThumbRepository : IThumbRepository
    {
        private readonly ILogger<ThumbRepository> logger;
        private readonly IBucketReader bucketReader;
        private readonly IOptionsMonitor<ThumbsSettings> settings;
        private readonly IThumbReorganiser thumbReorganiser;

        public ThumbRepository(
            ILogger<ThumbRepository> logger,
            IBucketReader bucketReader,
            IOptionsMonitor<ThumbsSettings> settings,
            IThumbReorganiser thumbReorganiser)
        {
            this.logger = logger;
            this.bucketReader = bucketReader;
            this.settings = settings;
            this.thumbReorganiser = thumbReorganiser;
        }

        public async Task<ThumbnailResponse> GetThumbnail(int customerId, int spaceId, ImageRequest imageRequest)
        {
            var sizeCandidate = await GetThumbnailSizeCandidate(customerId, spaceId, imageRequest);
            
            if (sizeCandidate == null) return ThumbnailResponse.Empty;
            
            if (sizeCandidate.KnownSize)
            {
                var location =
                    GetObjectInBucket(customerId, spaceId, imageRequest, sizeCandidate.LongestEdge!.Value);
                var objectFromBucket = await bucketReader.GetObjectFromBucket(location);
                return ThumbnailResponse.ExactSize(objectFromBucket.Stream);
            }

            if (!settings.CurrentValue.Resize)
            {
                logger.LogDebug("Could not find thumbnail for '{Path}' and resizing disabled",
                    imageRequest.OriginalPath);
                return ThumbnailResponse.Empty;
            }
            
            var resizableSize = (ResizableSize) sizeCandidate;
            
            // First try larger size
            if (resizableSize.LargerSize != null)
            {
                var downscaled = await ResizeThumbnail(customerId, spaceId, imageRequest, resizableSize.LargerSize,
                    resizableSize.Ideal);
                if (downscaled != null) return ThumbnailResponse.Resized(downscaled);
            }

            // Then try smaller size if allowed
            if (resizableSize.SmallerSize != null && settings.CurrentValue.Upscale)
            {
                var resizeThumbnail = await ResizeThumbnail(customerId, spaceId, imageRequest, resizableSize.SmallerSize,
                    resizableSize.Ideal, settings.CurrentValue.UpscaleThreshold);
                return ThumbnailResponse.Resized(resizeThumbnail);
            }

            return ThumbnailResponse.Empty;
        }

        public async Task<SizeCandidate?> GetThumbnailSizeCandidate(int customerId, int spaceId,
            ImageRequest imageRequest)
        {
            var openSizes = await GetOpenSizes(customerId, spaceId, imageRequest);
            if (openSizes == null) return null;
            
            var sizes = openSizes.Select(Size.FromArray).ToList();

            var sizeCandidate = ThumbnailCalculator.GetCandidate(sizes, imageRequest, settings.CurrentValue.Resize);
            return sizeCandidate;
        }

        public async Task<List<int[]>?> GetOpenSizes(int customerId, int spaceId, ImageRequest imageRequest)
        {
            var newLayoutResult = await EnsureNewLayout(customerId, spaceId, imageRequest);
            if (newLayoutResult == ReorganiseResult.AssetNotFound)
            {
                logger.LogDebug("Requested asset not found for '{OriginalPath}'", imageRequest.OriginalPath);
                return null;
            }

            ObjectInBucket sizesList = new(
                settings.CurrentValue.ThumbsBucket,
                StorageKeyGenerator.GetSizesJsonPath(GetKeyRoot(customerId, spaceId, imageRequest))
            );
            
            await using var stream = (await bucketReader.GetObjectFromBucket(sizesList)).Stream;
            if (stream == null)
            {
                logger.LogError("Could not find sizes file for request '{OriginalPath}'", imageRequest.OriginalPath);
                return null;
            }
            
            var serializer = new JsonSerializer();
            using var sr = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(sr);
            var thumbnailSizes = serializer.Deserialize<ThumbnailSizes>(jsonTextReader);
            return thumbnailSizes.Open;
        }

        private ObjectInBucket GetObjectInBucket(int customerId, int spaceId, ImageRequest imageRequest,
            int longestEdge)
            => new(settings.CurrentValue.ThumbsBucket,
                $"{GetKeyRoot(customerId, spaceId, imageRequest)}open/{longestEdge}.jpg");

        private string GetKeyRoot(int customerId, int spaceId, ImageRequest imageRequest) 
            => $"{StorageKeyGenerator.GetStorageKey(customerId, spaceId, imageRequest.Identifier)}/";

        private Task<ReorganiseResult> EnsureNewLayout(int customerId, int spaceId, ImageRequest imageRequest)
        {
            var currentSettings = this.settings.CurrentValue;
            if (!currentSettings.EnsureNewThumbnailLayout)
            {
                return Task.FromResult(ReorganiseResult.Unknown);
            }

            var rootKey = new ObjectInBucket(
                currentSettings.ThumbsBucket,
                GetKeyRoot(customerId, spaceId, imageRequest));

            return thumbReorganiser.EnsureNewLayout(rootKey);
        }

        private async Task<Stream?> ResizeThumbnail(int customerId, int spaceId, ImageRequest imageRequest,
            Size toResize, Size idealSize, int? maxDifference = 0)
        {
            // if upscaling, verify % difference isn't too great
            if ((maxDifference ?? 0) > 0 && idealSize.MaxDimension > toResize.MaxDimension)
            {
                var difference = (idealSize.MaxDimension / (double)toResize.MaxDimension) * 100;
                if (difference > maxDifference!.Value)
                {
                    logger.LogDebug("The next smallest thumbnail {ToResize} breaks the threshold for '{Path}'",
                        toResize.ToString(), imageRequest.OriginalPath);
                    return null;
                }
            }

            // we now have a candidate size - resize that and return
            logger.LogDebug("Resize the {Size} thumbnail for {Path}", toResize.MaxDimension,
                imageRequest.OriginalPath);

            var key = GetObjectInBucket(customerId, spaceId, imageRequest, toResize.MaxDimension);
            var thumbnail = (await bucketReader.GetObjectFromBucket(key)).Stream;
            var memStream = new MemoryStream();
            using var image = await Image.LoadAsync(thumbnail);
            image.Mutate(x => x.Resize(idealSize.Width, idealSize.Height, KnownResamplers.Lanczos3));
            await image.SaveAsync(memStream, new JpegEncoder());

            return memStream;
        }
    }
}
