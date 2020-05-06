using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Storage;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    public class AssetToS3 : IAssetMover<AssetInBucket>
    {
        private readonly IAssetMover<AssetOnDisk> assetMover;
        private readonly IBucketReader bucketReader;
        private readonly EngineSettings engineSettings;
        private readonly ILogger<AssetToS3> logger;

        public AssetToS3(
            IAssetMover<AssetOnDisk> assetMover,
            IOptionsMonitor<EngineSettings> engineSettings,
            IBucketReader bucketReader,
            ILogger<AssetToS3> logger)
        {
            this.assetMover = assetMover;
            this.bucketReader = bucketReader;
            this.engineSettings = engineSettings.CurrentValue;
            this.logger = logger;
        }
        
        public Task<AssetInBucket> CopyAsset(Asset asset, string destinationTemplate, bool verifySize,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
        {
            // TODO - general error handling, logging, check success results from bucketReader
            var targetUri = string.Format(destinationTemplate, asset.Customer, asset.Space, asset.GetUniqueName());
            var target = RegionalisedObjectInBucket.Parse(targetUri);
            
            if (ShouldCopyBucketToBucket(asset, customerOriginStrategy))
            {
                return CopyBucketToBucket(asset, destinationTemplate, target, cancellationToken);
            }

            return IndirectCopyBucketToBucket(asset, destinationTemplate, verifySize, customerOriginStrategy, target, cancellationToken);
        }

        private bool ShouldCopyBucketToBucket(Asset asset, CustomerOriginStrategy customerOriginStrategy)
        {
            var customerOverride = GetCustomerOverrideSettings(asset.Customer);
            return customerOverride.FullBucketAccess && customerOriginStrategy.Strategy == OriginStrategy.S3Ambient;
        }

        private CustomerOverridesSettings GetCustomerOverrideSettings(int customerId)
            => engineSettings.GetCustomerSettings(customerId);
        
        private async Task<AssetInBucket> CopyBucketToBucket(Asset asset, string destination,
            ObjectInBucket target, CancellationToken cancellationToken)
        {
            var source = RegionalisedObjectInBucket.Parse(asset.GetIngestOrigin());
            // TODO - throw if source null (couldn't be parsed)?

            // copy S3-S3
            var copyResult = await bucketReader.CopyLargeFileBetweenBuckets(source, target, cancellationToken);

            // TODO - contentType
            return new AssetInBucket(asset.Id, copyResult.Value ?? 0, destination, "");
        }
        
        private async Task<AssetInBucket> IndirectCopyBucketToBucket(Asset asset, string destination, bool verifySize,
            CustomerOriginStrategy customerOriginStrategy, ObjectInBucket target, CancellationToken cancellationToken)
        {
            var diskDestination = "";
            var assetOnDisk = await assetMover.CopyAsset(asset, diskDestination, verifySize, customerOriginStrategy,
                cancellationToken);

            var success = await bucketReader.WriteLargeFileToBucket(target, diskDestination, assetOnDisk.ContentType,
                cancellationToken);

            return new AssetInBucket(asset.Id, assetOnDisk.AssetSize, destination, assetOnDisk.ContentType);
        }
    }
}