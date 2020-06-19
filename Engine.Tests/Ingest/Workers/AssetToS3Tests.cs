using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Core;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Storage;
using DLCS.Test.Helpers.Settings;
using Engine.Ingest.Workers;
using Engine.Settings;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Engine.Tests.Ingest.Workers
{
    public class AssetToS3Tests
    {
        private readonly IAssetMover diskMover;
        private readonly IBucketReader bucketReader;
        private readonly EngineSettings engineSettings;
        private readonly AssetToS3 sut;
        
        public AssetToS3Tests()
        {
            diskMover = A.Fake<IAssetMover>();
            bucketReader = A.Fake<IBucketReader>();
            var c = Path.DirectorySeparatorChar;
            engineSettings = new EngineSettings
            {
                CustomerOverrides = new Dictionary<string, CustomerOverridesSettings>
                {
                    ["99"] = new CustomerOverridesSettings{FullBucketAccess = true},
                },
                TimebasedIngest = new TimebasedIngestSettings
                {
                    SourceTemplate = "{customer}",
                    ProcessingFolder = $".{c}scratch{c}",
                }
            };
            var optionsMonitor = OptionsHelpers.GetOptionsMonitor(engineSettings);
            sut = new AssetToS3(type => diskMover, optionsMonitor, bucketReader, NullLogger<AssetToS3>.Instance);
        }

        [Fact]
        public async Task CopyAsset_CopiesDirectS3ToS3_IfS3AmbientAndFullBucketAccess()
        {
            // Arrange
            var asset = new Asset
            {
                Customer = 99, Space = 1, Id = "balrog",
                Origin = "s3://eu-west-1/origin/large_file.mov"
            };
            const string destinationTemplate = "s3://eu-west-1/fantasy/";
            var originStrategy = new CustomerOriginStrategy
            {
                Strategy = OriginStrategy.S3Ambient
            };

            A.CallTo(() => bucketReader.CopyLargeFileBetweenBuckets(A<ObjectInBucket>._, A<ObjectInBucket>._,
                    A<Func<long, Task<bool>>>._, A<CancellationToken>._))
                .Returns(ResultStatus<long?>.Successful(100));
            
            var ct = new CancellationToken();

            // Act
            await sut.CopyAsset(asset, destinationTemplate, true, originStrategy, ct);

            // Assert
            A.CallTo(() => bucketReader.CopyLargeFileBetweenBuckets(
                    A<ObjectInBucket>.That.Matches(o => o.ToString() == "origin:::large_file.mov"),
                    A<ObjectInBucket>.That.Matches(o => o.ToString() == "fantasy:::99/1/balrog"),
                    A<Func<long, Task<bool>>>._, ct))
                .MustHaveHappened();
        }
        
        [Fact]
        public async Task CopyAsset_ReturnsExpected_AfterDirectS3ToS3()
        {
            // Arrange
            const string mediaType = "video/quicktime";
            const long assetSize = 1024;
            
            var asset = new Asset
            {
                Customer = 99, Space = 1, Id = "balrog",
                Origin = "s3://eu-west-1/origin/large_file.mov",
                MediaType = mediaType
            };
            const string destinationTemplate = "s3://eu-west-1/fantasy/";
            var originStrategy = new CustomerOriginStrategy
            {
                Strategy = OriginStrategy.S3Ambient
            };
            
            A.CallTo(() => bucketReader.CopyLargeFileBetweenBuckets(A<ObjectInBucket>._, A<ObjectInBucket>._,
                    A<Func<long, Task<bool>>>._, A<CancellationToken>._))
                .Returns(ResultStatus<long?>.Successful(assetSize));

            var expected = new AssetFromOrigin("balrog", assetSize, "99/1/balrog", mediaType);

            var ct = new CancellationToken();

            // Act
            var actual = await sut.CopyAsset(asset, destinationTemplate, true, originStrategy, ct);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CopyAsset_ThrowsIfDirectCopyFails()
        {
            // Arrange
            var asset = new Asset
            {
                Customer = 99, Space = 1, Id = "balrog",
                Origin = "s3://eu-west-1/origin/large_file.mov"
            };
            const string destinationTemplate = "s3://eu-west-1/fantasy/";
            var originStrategy = new CustomerOriginStrategy
            {
                Strategy = OriginStrategy.S3Ambient
            };

            A.CallTo(() => bucketReader.CopyLargeFileBetweenBuckets(A<ObjectInBucket>._, A<ObjectInBucket>._,
                    A<Func<long, Task<bool>>>._, A<CancellationToken>._))
                .Returns(ResultStatus<long?>.Unsuccessful(100));
            
            var ct = new CancellationToken();

            // Act
            Func<Task> action = () => sut.CopyAsset(asset, destinationTemplate, true, originStrategy, ct);

            // Assert
            action.Should().Throw<ApplicationException>();
        }

        [Theory]
        [InlineData(OriginStrategy.Default, 99)]
        [InlineData(OriginStrategy.BasicHttp, 99)]
        [InlineData(OriginStrategy.SFTP, 99)]
        [InlineData(OriginStrategy.S3Ambient, 90)]
        public async Task CopyAsset_CopiesToDisk_IfNotS3AmbientAndFullBucketAccess(OriginStrategy strategy,
            int customerId)
        {
            // Arrange
            var asset = new Asset
            {
                Customer = customerId, Space = 1, Id = "balrog",
                Origin = "s3://eu-west-1/origin/large_file.mov"
            };
            const string destinationTemplate = "s3://eu-west-1/fantasy/";
            var originStrategy = new CustomerOriginStrategy
            {
                Strategy = strategy
            };
            var ct = new CancellationToken();

            var assetFromOrigin = new AssetFromOrigin();
            assetFromOrigin.FileTooLarge();
            A.CallTo(() => diskMover.CopyAsset(asset, A<string>._, true, originStrategy, A<CancellationToken>._))
                .Returns(assetFromOrigin);

            // Act
            await sut.CopyAsset(asset, destinationTemplate, true, originStrategy, ct);

            // Assert
            A.CallTo(() => diskMover.CopyAsset(asset, A<string>._, true, originStrategy, A<CancellationToken>._))
                .MustHaveHappened();
        }
        
        [Fact]
        public async Task CopyAsset_CopiesFromDiskToBucket_IfNotS3Ambient()
        {
            // Arrange
            var asset = new Asset
            {
                Customer = 1, Space = 1, Id = "balrog",
                Origin = "s3://eu-west-1/origin/large_file.mov"
            };
            const string destinationTemplate = "s3://eu-west-1/fantasy/";
            var originStrategy = new CustomerOriginStrategy
            {
                Strategy = OriginStrategy.Default
            };
            var ct = new CancellationToken();

            var assetOnDisk = new AssetFromOrigin("balrog", 1234, "/on/disk", "video/mpeg");
            A.CallTo(() => diskMover.CopyAsset(asset, A<string>._, true, originStrategy, A<CancellationToken>._))
                .Returns(assetOnDisk);

            A.CallTo(() => bucketReader.WriteLargeFileToBucket(A<ObjectInBucket>._, A<string>._, A<string>._, ct))
                .Returns(true);

            // Act
            await sut.CopyAsset(asset, destinationTemplate, true, originStrategy, ct);

            // Assert
            A.CallTo(() => bucketReader.WriteLargeFileToBucket(
                    A<ObjectInBucket>.That.Matches(o => o.ToString() == "fantasy:::1/1/balrog"),
                    "/on/disk",
                    "video/mpeg",
                    ct))
                .MustHaveHappened();
        }
        
        [Fact]
        public async Task CopyAsset_ReturnsExpected_AfterInDirectS3ToS3()
        {
            // Arrange
            const string mediaType = "video/mpeg";
            const long assetSize = 1024;
            
            var asset = new Asset
            {
                Customer = 9, Space = 1, Id = "balrog",
                Origin = "s3://eu-west-1/origin/large_file.mov",
                MediaType = mediaType
            };
            const string destinationTemplate = "s3://eu-west-1/fantasy/";
            var originStrategy = new CustomerOriginStrategy
            {
                Strategy = OriginStrategy.S3Ambient
            };
            
            var expected = new AssetFromOrigin("balrog", assetSize, "9/1/balrog", mediaType);

            var ct = new CancellationToken();
            var assetOnDisk = new AssetFromOrigin("balrog", assetSize, "/on/disk", mediaType);
            A.CallTo(() => diskMover.CopyAsset(asset, A<string>._, true, originStrategy, A<CancellationToken>._))
                .Returns(assetOnDisk);

            A.CallTo(() => bucketReader.WriteLargeFileToBucket(A<ObjectInBucket>._, A<string>._, A<string>._, ct))
                .Returns(true);

            // Act
            var actual = await sut.CopyAsset(asset, destinationTemplate, true, originStrategy, ct);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void CopyAsset_ThrowsIfUploadtoS3Fails_IfNotS3Ambient()
        {
            // Arrange
            var asset = new Asset
            {
                Customer = 9, Space = 1, Id = "balrog",
                Origin = "s3://eu-west-1/origin/large_file.mov"
            };
            const string destinationTemplate = "s3://eu-west-1/fantasy/";
            var originStrategy = new CustomerOriginStrategy
            {
                Strategy = OriginStrategy.S3Ambient
            };

            var ct = new CancellationToken();
            var assetOnDisk = new AssetFromOrigin("balrog", 1234, "/on/disk", "video/mpeg");
            A.CallTo(() => diskMover.CopyAsset(asset, A<string>._, true, originStrategy, A<CancellationToken>._))
                .Returns(assetOnDisk);

            // Act
            Func<Task> action = () => sut.CopyAsset(asset, destinationTemplate, true, originStrategy, ct);

            // Assert
            action.Should().Throw<ApplicationException>();
        }
    }
}