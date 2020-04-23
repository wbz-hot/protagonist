﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Storage;
using DLCS.Repository.Settings;
using DLCS.Repository.Storage;
using IIIF.ImageApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DLCS.Repository.Assets
{
    public class ThumbRepository : IThumbRepository
    {
        private readonly ILogger<ThumbRepository> logger;
        private readonly IBucketReader bucketReader;
        private readonly IOptionsMonitor<ThumbsSettings> settings;
        private readonly IThumbLayoutManager thumbLayoutManager;

        public ThumbRepository(
            ILogger<ThumbRepository> logger,
            IBucketReader bucketReader,
            IOptionsMonitor<ThumbsSettings> settings,
            IThumbLayoutManager thumbLayoutManager)
        {
            this.logger = logger;
            this.bucketReader = bucketReader;
            this.settings = settings;
            this.thumbLayoutManager = thumbLayoutManager;
        }

        public async Task<ObjectInBucket> GetThumbLocation(int customerId, int spaceId, ImageRequest imageRequest)
        {
            await EnsureNewLayout(customerId, spaceId, imageRequest);
            int longestEdge = 0;
            if (imageRequest.Size.Width > 0 && imageRequest.Size.Height > 0)
            {
                // We don't actually need to check imageRequest.Size.Confined (!w,h) because same logic applies...
                longestEdge = Math.Max(imageRequest.Size.Width, imageRequest.Size.Height);
            }
            else
            {
                // we need to know the sizes of things...
                var sizes = await GetSizes(customerId, spaceId, imageRequest);
                if (imageRequest.Size.Width > 0)
                {
                    foreach (var size in sizes)
                    {
                        if (size[0] == imageRequest.Size.Width)
                        {
                            longestEdge = Math.Max(size[0], size[1]);
                            break;
                        }
                    }
                }
                if (imageRequest.Size.Height > 0)
                {
                    foreach (var size in sizes)
                    {
                        if (size[1] == imageRequest.Size.Height)
                        {
                            longestEdge = Math.Max(size[0], size[1]);
                            break;
                        }
                    }
                }

                if (imageRequest.Size.Max)
                {
                    longestEdge = Math.Max(sizes[0][0], sizes[0][1]);
                }
            }

            return new ObjectInBucket
            (
                settings.CurrentValue.ThumbsBucket,
                StorageKeyGenerator.GetConfinedSquarePath(GetKeyRoot(customerId, spaceId, imageRequest), longestEdge,
                    true)
            );
        }

        public async Task<List<int[]>> GetSizes(int customerId, int spaceId, ImageRequest imageRequest)
        {
            await EnsureNewLayout(customerId, spaceId, imageRequest);

            ObjectInBucket sizesList = new ObjectInBucket
            (
                settings.CurrentValue.ThumbsBucket,
                string.Concat(GetKeyRoot(customerId, spaceId, imageRequest), ThumbsSettings.Constants.SizesJsonKey)
            );
            
            await using var stream = await bucketReader.GetObjectContentFromBucket(sizesList);
            if (stream == null)
            {
                logger.LogError("Could not find sizes file for request '{OriginalPath}'", imageRequest.OriginalPath);
                return new List<int[]>();
            }
            
            var serializer = new JsonSerializer();
            using var sr = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(sr);
            var thumbnailSizes = serializer.Deserialize<ThumbnailSizes>(jsonTextReader);
            return thumbnailSizes.Open;
        }

        private static string GetKeyRoot(int customerId, int spaceId, ImageRequest imageRequest) 
            => $"{StorageKeyGenerator.GetStorageKey(customerId, spaceId, imageRequest.Identifier)}/";

        private Task EnsureNewLayout(int customerId, int spaceId, ImageRequest imageRequest)
        {
            var currentSettings = this.settings.CurrentValue;
            if (!currentSettings.EnsureNewThumbnailLayout)
            {
                return Task.CompletedTask;
            }

            var rootKey = new ObjectInBucket
            (
                currentSettings.ThumbsBucket,
                GetKeyRoot(customerId, spaceId, imageRequest)
            );

            return thumbLayoutManager.EnsureNewLayout(rootKey);
        }
    }
}
