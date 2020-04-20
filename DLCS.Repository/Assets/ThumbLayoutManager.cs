using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DLCS.Core.Collections;
using DLCS.Core.Threading;
using DLCS.Model.Assets;
using DLCS.Model.Storage;
using IIIF;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DLCS.Repository.Assets
{
    public class ThumbLayoutManager : IThumbLayoutManager
    {
        private readonly IBucketReader bucketReader;
        private readonly ILogger<ThumbRepository> logger;
        private readonly IAssetRepository assetRepository;
        private readonly IAssetPolicyRepository assetPolicyRepository;
        private readonly AsyncKeyedLock asyncLocker = new AsyncKeyedLock();
        private static Regex BoundedThumbRegex = new Regex("^[0-9]+.jpg$");

        public ThumbLayoutManager(
            IBucketReader bucketReader,
            ILogger<ThumbRepository> logger,
            IAssetRepository assetRepository,
            IAssetPolicyRepository assetPolicyRepository )
        {
            this.bucketReader = bucketReader;
            this.logger = logger;
            this.assetRepository = assetRepository;
            this.assetPolicyRepository = assetPolicyRepository;
        }

        public async Task EnsureNewLayout(ObjectInBucket rootKey)
        {
            // Create lock on rootKey unique value (bucket + target key)
            using var processLock = await asyncLocker.LockAsync(rootKey.ToString());
            
            var keysInTargetBucket = await bucketReader.GetMatchingKeys(rootKey);
            if (HasCurrentLayout(rootKey, keysInTargetBucket))
            {
                logger.LogDebug("{RootKey} has expected current layout", rootKey);
                return;
            }

            // under full/ we will find some sizes, but not the largest.
            // the largest is at low.jpg in the "root".
            // trouble is we do not know how big it is!
            // we'll need to fetch the image dimensions from the database, the Thumbnail policy the image was created with, and compute the sizes.
            // Then sanity check them against the known sizes.
            
            var asset = await assetRepository.GetAsset(rootKey.Key.TrimEnd('/'));

            //404 Not Found Asset
            if (asset == null)
                return;

            var policy = await assetPolicyRepository.GetThumbnailPolicy(asset.ThumbnailPolicy);
            
            var realSize = new Size(asset.Width, asset.Height);
            var boundingSquares = policy.Sizes.OrderByDescending(i => i).ToList();

            var thumbnailSizes = new ThumbnailSizes(boundingSquares.Count);
            thumbnailSizes.SetMaxAvailableSize(GetMaxAvailableThumb(asset, policy));
            foreach (int boundingSquare in boundingSquares)
            {
                var thumb = Size.Confine(boundingSquare, realSize);
                thumbnailSizes.Add(thumb);
            }

            // All the thumbnail jpgs will already exist and need copied up to root
            await CreateConfinedSquareThumbnailsFromLegacy(rootKey, boundingSquares, thumbnailSizes);

            // Create sizes json file last, as this dictates whether this process will be attempted again
            await CreateSizesJson(rootKey, thumbnailSizes);

            // Clean up legacy format from before /open /auth paths
            await CleanupRootConfinedSquareThumbs(rootKey, keysInTargetBucket);
        }

        public async Task CreateNewThumbs(Asset asset, IEnumerable<ThumbOnDisk> thumbsToProcess,
            ObjectInBucket rootKey)
        {
            var thumbOnDisks = thumbsToProcess as ThumbOnDisk[] ?? thumbsToProcess.ToArray();
            if (thumbOnDisks.IsNullOrEmpty()) return;
            
            using var processLock = await asyncLocker.LockAsync(rootKey.ToString());

            // TODO - will this be a 404 if not found?
            var keysInTargetBucket = await bucketReader.GetMatchingKeys(rootKey);
            
            // /1/2/imagename
            var bucketKey = rootKey.Key;

            var thumbnailSizes = new ThumbnailSizes(thumbOnDisks.Length);
            var maxAvailableThumb = GetMaxAvailableThumb(asset, asset.FullThumbnailPolicy);
            
            // dictionary of newPath : legacyPaths[]
            var legacyCopies = new Dictionary<string, string[]>(thumbOnDisks.Length);
            foreach (var thumbCandidate in thumbOnDisks)
            {
                var thumb = new Size(thumbCandidate.Width, thumbCandidate.Height);
                bool isOpen = false;

                if (thumb.IsConfinedWithin(maxAvailableThumb))
                {
                    thumbnailSizes.AddOpen(thumb);
                    isOpen = true;
                }
                else
                {
                    thumbnailSizes.AddAuth(thumb);
                    isOpen = false;
                }

                var thumbKey = ThumbnailKeys.GetConfinedSquarePath(bucketKey, thumb, isOpen);
                var objectInBucket = rootKey.CloneWithKey(thumbKey);
                
                // upload confined square thumb (new)
                await bucketReader.WriteFileToBucket(objectInBucket, thumbCandidate.Path);

                // build up a list of in-bucket copies to do to maintain legacy
                legacyCopies.Add(
                    thumbKey,
                    GetPathsToCopyTo(bucketKey, maxAvailableThumb.MaxDimension, thumb)
                );
            }

            await CreateSizesJson(rootKey, thumbnailSizes);
            
            await CreateLegacyThumbsFromConfinedSquare(rootKey, legacyCopies);

            // Clean up legacy format from before /open /auth paths
            await CleanupRootConfinedSquareThumbs(rootKey, keysInTargetBucket);
        }

        private static bool HasCurrentLayout(ObjectInBucket rootKey, string[] keysInTargetBucket)
            => keysInTargetBucket.Contains(ThumbnailKeys.GetSizesJsonPath(rootKey.Key));

        private static Size GetMaxAvailableThumb(Asset asset, ThumbnailPolicy policy)
        {
            var _ = asset.GetAvailableThumbSizes(policy, out var maxDimensions);
            return Size.Square(maxDimensions.maxBoundedSize);
        }

        private async Task CreateConfinedSquareThumbnailsFromLegacy(ObjectInBucket rootKey, List<int> boundingSquares, ThumbnailSizes thumbnailSizes)
        {
            var copyTasks = new List<Task>(thumbnailSizes.Count);
            
            // low.jpg becomes the first in this list
            var largestSize = boundingSquares[0];
            var largestIsOpen = thumbnailSizes.Auth.IsNullOrEmpty();
            copyTasks.Add(bucketReader.CopyWithinBucket(rootKey.Bucket,
                ThumbnailKeys.GetLowPath(rootKey.Key),
                ThumbnailKeys.GetConfinedSquarePath(rootKey.Key, largestSize, largestIsOpen)));
            
            copyTasks.AddRange(ProcessCopyThumbBatch(rootKey, thumbnailSizes.Auth, false, largestSize));
            copyTasks.AddRange(ProcessCopyThumbBatch(rootKey, thumbnailSizes.Open, true, largestSize));
            
            await Task.WhenAll(copyTasks);
        }

        private IEnumerable<Task> ProcessCopyThumbBatch(ObjectInBucket rootKey, IEnumerable<int[]> thumbnailSizes,
            bool isOpen, int largestSize)
        {
            foreach (var wh in thumbnailSizes)
            {
                var size = Size.FromArray(wh);
                if (size.MaxDimension == largestSize) continue;

                yield return bucketReader.CopyWithinBucket(rootKey.Bucket,
                    ThumbnailKeys.GetThumbnailWHPath(rootKey.Key, size),
                    ThumbnailKeys.GetConfinedSquarePath(rootKey.Key, size, isOpen));
            }
        }

        private async Task CreateSizesJson(ObjectInBucket rootKey, ThumbnailSizes thumbnailSizes)
        {
            var sizesDest = rootKey.CloneWithKey(ThumbnailKeys.GetSizesJsonPath(rootKey.Key));
            await bucketReader.WriteToBucket(sizesDest, JsonConvert.SerializeObject(thumbnailSizes), "application/json");
        }
        
        private string[] GetPathsToCopyTo(string key, int maxSize, Size thumb)
        {
            // If this is biggest, copy to low.jpg
            if (thumb.MaxDimension == maxSize)
            {
                return new[] {ThumbnailKeys.GetLowPath(key)};
            }

            // else copy to "w,h" and "w," paths
            return new[] {ThumbnailKeys.GetThumbnailWPath(key, thumb), ThumbnailKeys.GetThumbnailWHPath(key, thumb)};
        }
        
        private async Task CreateLegacyThumbsFromConfinedSquare(ObjectInBucket rootKey, Dictionary<string,string[]> legacyCopies)
        {
            List<Task> copyOperations = new List<Task>(legacyCopies.Count * 2);
            var bucket = rootKey.Bucket;
            
            foreach (var (key, legacyKeys) in legacyCopies)
            {
                foreach (var dest in legacyKeys)
                {
                    logger.LogTrace("Copying '{key}' to '{dest}' (bucket: '{bucket}'", key, dest, bucket);
                    copyOperations.Add(bucketReader.CopyWithinBucket(bucket, key, dest));
                }
            }

            await Task.WhenAll(copyOperations);
        }

        private async Task CleanupRootConfinedSquareThumbs(ObjectInBucket rootKey, string[] s3ObjectKeys)
        {
            // This is an interim method to clean up the first implementation of /thumbs/ handling
            // which created all thumbs at root and sizes.json, rather than s.json
            // We output s.json now. Previously this was sizes.json
            const string oldSizesJsonKey = "sizes.json";
            
            if (s3ObjectKeys.IsNullOrEmpty()) return;
            
            List<ObjectInBucket> toDelete = new List<ObjectInBucket>(s3ObjectKeys.Length);
            
            foreach (var key in s3ObjectKeys)
            {
                string item = key.Replace(rootKey.Key, string.Empty);
                if (BoundedThumbRegex.IsMatch(item) || item == oldSizesJsonKey)
                {
                    logger.LogDebug($"Deleting legacy confined-thumb object: '{key}'");
                    toDelete.Add(new ObjectInBucket(rootKey.Bucket, key));
                }
            }

            if (toDelete.Count > 0)
            {
                await bucketReader.DeleteFromBucket(toDelete.ToArray());
            }
        }

        public void DeleteOldLayout()
        {
            throw new NotImplementedException("Not yet! Need to be sure of all the others first!");
        }
    }
}
