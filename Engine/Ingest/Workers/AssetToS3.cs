using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Core.Guard;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Storage;
using DLCS.Repository.Storage;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    /// <summary>
    /// Class for copying asset from origin to S3 bucket.
    /// </summary>
    public class AssetToS3 : IAssetMover
    {
        private readonly IAssetMover diskMover;
        private readonly IBucketReader bucketReader;
        private readonly EngineSettings engineSettings;
        private readonly ILogger<AssetToS3> logger;

        public AssetToS3(
            AssetMoverResolver assetMoverResolver,
            IOptionsMonitor<EngineSettings> engineSettings,
            IBucketReader bucketReader,
            ILogger<AssetToS3> logger)
        {
            diskMover = assetMoverResolver(AssetMoveType.Disk);
            this.bucketReader = bucketReader;
            this.engineSettings = engineSettings.CurrentValue;
            this.logger = logger;
        }
        
        /// <summary>
        /// Copy asset from Origin to S3 bucket.
        /// Configuration determines if this is a direct S3-S3 copy, or S3-disk-S3.
        /// </summary>
        /// <param name="asset"><see cref="Asset"/> to be copied</param>
        /// <param name="destinationTemplate">String representing destinations S3 bucket</param>
        /// <param name="verifySize">if True, size is validated that it does not exceed allowed size.</param>
        /// <param name="customerOriginStrategy"><see cref="CustomerOriginStrategy"/> to use to fetch item.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="AssetFromOrigin"/> containing new location, size etc</returns>
        public Task<AssetFromOrigin> CopyAsset(Asset asset, string destinationTemplate, bool verifySize,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
        {
            var target = GetBucketTarget(asset, destinationTemplate);

            if (ShouldCopyBucketToBucket(asset, customerOriginStrategy))
            {
                return CopyBucketToBucket(asset, target.Key!, verifySize, target, cancellationToken);
            }

            return IndirectCopyBucketToBucket(asset, target.Key!, verifySize, customerOriginStrategy, target,
                cancellationToken);
        }

        private static RegionalisedObjectInBucket GetBucketTarget(Asset asset, string destinationTemplate)
        {
            var storageKey = asset.GetStorageKey();
            var targetUri = $"{destinationTemplate}{storageKey}";
            var target = RegionalisedObjectInBucket.Parse(targetUri);
            return target.ThrowIfNull(nameof(target));
        }

        private bool ShouldCopyBucketToBucket(Asset asset, CustomerOriginStrategy customerOriginStrategy)
        {
            // TODO - FullBucketAccess for entire customer isn't granular enough
            var customerOverride = GetCustomerOverrideSettings(asset.Customer);
            return customerOverride.FullBucketAccess && customerOriginStrategy.Strategy == OriginStrategy.S3Ambient;
        }

        private CustomerOverridesSettings GetCustomerOverrideSettings(int customerId)
            => engineSettings.GetCustomerSettings(customerId);
        
        private async Task<AssetFromOrigin> CopyBucketToBucket(Asset asset, string location, bool verifySize,
            ObjectInBucket target, CancellationToken cancellationToken)
        {
            var source = RegionalisedObjectInBucket.Parse(asset.GetIngestOrigin());
            logger.LogDebug("Copying asset '{id}' directly from bucket to bucket. {source} - {dest}", asset.Id,
                source.GetS3QualifiedUri(), target);

            // copy S3-S3
            // TODO - verify content size using overload
            var copyResult = await bucketReader.CopyLargeFileBetweenBuckets(source, target,
                token: cancellationToken);

            if (!copyResult.Success)
            {
                throw new ApplicationException(
                    $"Failed to copy timebased asset {asset.Id} directly from '{asset.GetIngestOrigin()}' to {location}");
            }
            
            return new AssetFromOrigin(asset.Id, copyResult.Value ?? 0, location, asset.MediaType);
        }
        
        /// <summary>
        /// First copy asset from origin to disk, and then to final bucket destination.
        /// </summary>
        private async Task<AssetFromOrigin> IndirectCopyBucketToBucket(Asset asset, string location, bool verifySize,
            CustomerOriginStrategy customerOriginStrategy, ObjectInBucket target, CancellationToken cancellationToken)
        {
            logger.LogDebug("Copying asset '{id}' indirectly from bucket to bucket. {source} - {dest}", asset.Id,
                asset.GetIngestOrigin(), target);
            var diskDestination = GetDestination(asset);

            var assetOnDisk = await diskMover.CopyAsset(asset, diskDestination, verifySize, customerOriginStrategy,
                cancellationToken);

            if (assetOnDisk.FileExceedsAllowance)
            {
                var assetFromOrigin =
                    new AssetFromOrigin(asset.Id, assetOnDisk.AssetSize, location, assetOnDisk.ContentType);
                assetFromOrigin.FileTooLarge();
                return assetFromOrigin;
            }

            var success = await bucketReader.WriteLargeFileToBucket(target, assetOnDisk.Location, assetOnDisk.ContentType,
                cancellationToken);

            try
            {
                // TODO - do we want to do this?
                File.Delete(diskDestination);
            }
            catch (Exception)
            {
                // no-op, scavenger will get 
            }

            if (!success)
            {
                throw new ApplicationException(
                    $"Failed to copy timebased asset {asset.Id} indirectly from '{asset.GetIngestOrigin()}' to {location}");
            }
            
            return new AssetFromOrigin(asset.Id, assetOnDisk.AssetSize, location, assetOnDisk.ContentType);
        }

        private string GetDestination(Asset asset)
        {
            var diskDestination = TemplatedFolders.GenerateTemplate(engineSettings.TimebasedIngest.SourceTemplate,
                engineSettings.TimebasedIngest.ProcessingFolder, asset);
            Directory.CreateDirectory(diskDestination);
            return diskDestination;
        }
    }
}