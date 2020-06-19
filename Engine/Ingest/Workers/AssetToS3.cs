using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Storage;
using DLCS.Repository.Storage;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
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
        
        public Task<AssetFromOrigin> CopyAsset(Asset asset, string destinationTemplate, bool verifySize,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
        {
            // TODO - need to add something to make this unique here?
            var storageKey = asset.GetStorageKey();
            var targetUri = $"{destinationTemplate}{storageKey}";
            var target = RegionalisedObjectInBucket.Parse(targetUri);
            
            if (ShouldCopyBucketToBucket(asset, customerOriginStrategy))
            {
                return CopyBucketToBucket(asset, storageKey, verifySize, target, cancellationToken);
            }

            return IndirectCopyBucketToBucket(asset, storageKey, verifySize, customerOriginStrategy, target,
                cancellationToken);
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