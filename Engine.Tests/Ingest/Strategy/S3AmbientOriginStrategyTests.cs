﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Storage;
using DLCS.Test.Helpers;
using Engine.Ingest.Strategy;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Engine.Tests.Ingest.Strategy
{
    public class S3AmbientOriginStrategyTests
    {
        private readonly S3AmbientOriginStrategy sut;
        private readonly IBucketReader bucketReader;
        private readonly CustomerOriginStrategy customerOriginStrategy;

        public S3AmbientOriginStrategyTests()
        {
            bucketReader = A.Fake<IBucketReader>();

            sut = new S3AmbientOriginStrategy(bucketReader, new NullLogger<S3AmbientOriginStrategy>());
            customerOriginStrategy = new CustomerOriginStrategy
            {
                Strategy = OriginStrategy.S3Ambient
            };
        }

        [Fact]
        public async Task LoadAssetFromOrigin_ReturnsExpectedResponse_OnSuccess()
        {
            // Arrange
            const string contentType = "application/json";
            const long contentLength = 4324;
            var response = new ObjectFromBucket(new ObjectInBucket("bucket"),
                "this is a test".ToMemoryStream(),
                new ObjectInBucketHeaders
                {
                    ContentType = contentType,
                    ContentLength = contentLength
                }
            );
            
            const string originUri = "s3://eu-west-1/test-storage/2/1/ratts-of-the-capital";
            var objectInBucket =
                new RegionalisedObjectInBucket("test-storage", "2/1/ratts-of-the-capital", "eu-west-1");
            var regionalisedString = objectInBucket.ToString();
            A.CallTo(() =>
                bucketReader.GetObjectFromBucket(
                    A<ObjectInBucket>.That.Matches(a => a.ToString() == regionalisedString))).Returns(response);

            // Act
            var result = await sut.LoadAssetFromOrigin(new Asset {Origin = originUri}, customerOriginStrategy);
            
            // Assert
            A.CallTo(() =>
                    bucketReader.GetObjectFromBucket(
                        A<ObjectInBucket>.That.Matches(a => a.ToString() == regionalisedString)))
                .MustHaveHappened();
            result.Stream.Should().NotBeNull().And.Should().NotBe(Stream.Null);
            result.ContentLength.Should().Be(contentLength);
            result.ContentType.Should().Be(contentType);
        }
        
        [Fact]
        public async Task LoadAssetFromOrigin_UsesInitialOrigin_IfSpecified()
        {
            // Arrange
            const string originUri = "s3://eu-west-1/test-storage/2/1/ratts-of-the-capital";
            const string initialOrigin = "s3://us-east-1/test-storage/2/1/simon-ferocious";
            var objectInBucket =
                new RegionalisedObjectInBucket("test-storage", "2/1/simon-ferocious", "us-east-1");
            var initialOriginString = objectInBucket.ToString();
            
            // Act
            var asset = new Asset {Origin = originUri, InitialOrigin = initialOrigin};
            await sut.LoadAssetFromOrigin(asset, customerOriginStrategy);
            
            // Assert
            A.CallTo(() =>
                    bucketReader.GetObjectFromBucket(
                        A<ObjectInBucket>.That.Matches(a => a.ToString() == initialOriginString)))
                .MustHaveHappened();
        }
        
        [Fact]
        public async Task LoadAssetFromOrigin_HandlesNoContentLengthAndType()
        {
            // Arrange
            var response = new ObjectFromBucket(new ObjectInBucket("bucket"),
                "this is a test".ToMemoryStream(),
                new ObjectInBucketHeaders()
            );
            
            const string originUri = "s3://eu-west-1/test-storage/2/1/repelish";
            A.CallTo(() => bucketReader.GetObjectFromBucket(A<ObjectInBucket>._)).Returns(response);

            // Act
            var result = await sut.LoadAssetFromOrigin(new Asset {Origin = originUri}, customerOriginStrategy);
            
            // Assert
            result.Stream.Should().NotBeNull().And.Should().NotBe(Stream.Null);
            result.ContentLength.Should().BeNull();
            result.ContentType.Should().BeNull();
        }
        
        [Fact]
        public async Task LoadAssetFromOrigin_ReturnsNull_IfCallFails()
        {
            // Arrange
            const string originUri = "s3://eu-west-1/test-storage/2/1/repelish";
            A.CallTo(() => bucketReader.GetObjectFromBucket(A<ObjectInBucket>._))
                .ThrowsAsync(new Exception());
            
            // Act
            var result = await sut.LoadAssetFromOrigin(new Asset {Origin = originUri}, customerOriginStrategy);
            
            // Assert
            result.Should().BeNull();
        }
    }
}