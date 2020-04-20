﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DLCS.Core.Exceptions;
using DLCS.Model.Storage;
using DLCS.Repository.Storage.S3;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DLCS.Repository.Tests.Storage.S3
{
    public class BucketReaderTests
    {
        private readonly IAmazonS3 s3Client;
        private readonly BucketReader sut;
        
        public BucketReaderTests()
        {
            s3Client = A.Fake<IAmazonS3>();
            sut = new BucketReader(s3Client, new NullLogger<BucketReader>());
        }

        [Fact]
        public async Task GetObjectFromBucket_ReturnsFoundObjectAsStream()
        {
            // Arrange
            const string bucket = "MyBucket";
            const string key = "MyKey";
            const string bucketResponse = "This is a response from s3";
            
            var responseStream = new MemoryStream(Encoding.Default.GetBytes(bucketResponse));
            A.CallTo(() =>
                    s3Client.GetObjectAsync(
                        A<GetObjectRequest>.That.Matches(r => r.BucketName == bucket && r.Key == key),
                        A<CancellationToken>.Ignored))
                .Returns(new GetObjectResponse {ResponseStream = responseStream});

            var objectInBucket = new ObjectInBucket(bucket, key);

            // Act
            var targetStream = await sut.GetObjectFromBucket(objectInBucket);

            // Assert
            var memoryStream = new MemoryStream();
            await targetStream.CopyToAsync(memoryStream);
            
            var actual = Encoding.Default.GetString(memoryStream.ToArray());
            actual.Should().Be(bucketResponse);
        }
        
        [Fact]
        public async Task GetObjectFromBucket_ReturnsNull_IfKeyNotFound()
        {
            // Arrange
            A.CallTo(() =>
                    s3Client.GetObjectAsync(
                        A<GetObjectRequest>.Ignored,
                        A<CancellationToken>.Ignored))
                .ThrowsAsync(new AmazonS3Exception("uh-oh", ErrorType.Unknown, "123", "xxx-1", HttpStatusCode.NotFound));

            var objectInBucket = new ObjectInBucket("MyBucket", "MyKey");

            // Act
            var result = await sut.GetObjectFromBucket(objectInBucket);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(HttpStatusCode.Redirect)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public void GetObjectFromBucket_ThrowsHttpException_IfS3CopyFails_DueToNon404(HttpStatusCode statusCode)
        {
            // Arrange
            A.CallTo(() =>
                    s3Client.GetObjectAsync(
                        A<GetObjectRequest>.Ignored,
                        A<CancellationToken>.Ignored))
                .ThrowsAsync(new AmazonS3Exception("uh-oh", ErrorType.Unknown, "123", "xxx-1", statusCode));

            var objectInBucket = new ObjectInBucket("MyBucket", "MyKey");

            // Act
            Func<Task> action = () => sut.GetObjectFromBucket(objectInBucket);

            // Assert
            action.Should().Throw<HttpException>().Which.StatusCode.Should().Be(statusCode);
        }

        [Fact]
        public async Task WriteFileToBucket_ReturnsFalse_IfExceptionThrown()
        {
            // Arrange
            A.CallTo(() =>
                    s3Client.PutObjectAsync(
                        A<PutObjectRequest>.Ignored,
                        A<CancellationToken>.Ignored))
                .ThrowsAsync(new Exception("boom!"));
            
            var objectInBucket = new ObjectInBucket("MyBucket", "MyKey");
            
            // Act
            var result = await sut.WriteFileToBucket(objectInBucket, "/some/file.jpg");
            
            // Assert
            result.Should().BeFalse();
        }
        
        [Fact]
        public async Task WriteFileToBucket_ReturnsTrue_IfSuccess()
        {
            // Arrange
            var objectInBucket = new ObjectInBucket("MyBucket", "MyKey");
            
            // Act
            var result = await sut.WriteFileToBucket(objectInBucket, "/some/file.jpg");
            
            // Assert
            result.Should().BeTrue();
        }
    }
}