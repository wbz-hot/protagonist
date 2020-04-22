using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Storage;
using DLCS.Repository.Assets;
using DLCS.Repository.Settings;
using DLCS.Test.Helpers.Storage;
using DLCS.Test.Helpers.Web;
using Engine.Ingest;
using Engine.Ingest.Image;
using Engine.Ingest.Workers;
using Engine.Settings;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;

namespace Engine.Tests.Ingest.Image
{
    public class ImageProcessorTests
    {
        private readonly ControllableHttpMessageHandler httpHandler;
        private readonly TestBucketReader bucketReader;
        private readonly IThumbLayoutManager thumbLayoutManager;
        private readonly EngineSettings engineSettings;
        private readonly ImageProcessor sut;

        public ImageProcessorTests()
        {
            httpHandler = new ControllableHttpMessageHandler();
            bucketReader = new TestBucketReader("s3://storage-bucket");
            engineSettings = new EngineSettings
            {
                Thumbs = new ThumbsSettings
                {
                    StorageBucket = "s3://storage-bucket",
                    ThumbsBucket = "s3://thumbs-bucket"
                },
                S3Template = "s3://eu-west-1/storage-bucket/{0}/{1}/{2}",
                ScratchRoot = "/scratch/",
                ImageIngest = new ImageIngestSettings
                {
                    DestinationTemplate = "/{root}/godzilla/{customer}/{space}/{image}/output/",
                    ThumbsTemplate = "/{root}/godzilla/{customer}/{space}/{image}/output/thumb/"
                }
            };
            thumbLayoutManager = A.Fake<IThumbLayoutManager>();
            var optionsMonitor = A.Fake<IOptionsMonitor<EngineSettings>>();
            A.CallTo(() => optionsMonitor.CurrentValue).Returns(engineSettings);
            
            var httpClient = new HttpClient(httpHandler);
            httpClient.BaseAddress = new Uri("http://image-processor/");
            sut = new ImageProcessor(httpClient, bucketReader, thumbLayoutManager, optionsMonitor,
                new NullLogger<ImageProcessor>());
        }

        [Fact]
        public async Task ProcessImage_False_IfImageProcessorCallFails()
        {
            // Arrange
            httpHandler.SetResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var context = GetIngestionContext();

            // Act
            var result = await sut.ProcessImage(context);
            
            // Assert
            httpHandler.CallsMade.Should().ContainSingle(s => s == "http://image-processor/");
            result.Should().BeFalse();
            context.Asset.Should().NotBeNull();
        }

        [Fact]
        public async Task ProcessImage_UpdatesAssetSize()
        {
            // Arrange
            var imageProcessorResponse = new ImageProcessorResponseModel
            {
                Height = 1000,
                Width = 5000,
                Thumbs = new ThumbOnDisk[0]
            };

            var response = httpHandler.GetResponseMessage(JsonConvert.SerializeObject(imageProcessorResponse),
                HttpStatusCode.OK);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpHandler.SetResponse(response);
            
            var context = GetIngestionContext();

            // Act
            await sut.ProcessImage(context);
            
            // Assert
            context.Asset.Height.Should().Be(imageProcessorResponse.Height);
            context.Asset.Width.Should().Be(imageProcessorResponse.Width);
        }
        
        [Theory]
        [InlineData(true, OriginStrategy.Default)]
        [InlineData(true, OriginStrategy.BasicHttp)]
        [InlineData(true, OriginStrategy.SFTP)]
        [InlineData(false, OriginStrategy.S3Ambient)]
        public async Task ProcessImage_UploadsFileToBucket_AndSetsImageLocation_IfNotS3OptimisedStrategy(bool optimised, OriginStrategy strategy)
        {
            // Arrange
            var imageProcessorResponse = new ImageProcessorResponseModel {Thumbs = new ThumbOnDisk[0]};

            var response = httpHandler.GetResponseMessage(JsonConvert.SerializeObject(imageProcessorResponse),
                HttpStatusCode.OK);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpHandler.SetResponse(response);
            
            var context = GetIngestionContext();
            context.AssetFromOrigin.CustomerOriginStrategy = new CustomerOriginStrategy
            {
                Optimised = optimised,
                Strategy = strategy
            };

            const string expected = "s3://eu-west-1/storage-bucket/1/2/something";

            // Act
            await sut.ProcessImage(context);
            
            // Assert
            bucketReader.ShouldHaveKey("1/2/something").WithFilePath("./scratch/here.jpg");
            context.ImageLocation.S3.Should().Be(expected);
        }
        
        [Fact]
        public async Task ProcessImage_SetsImageLocation_WithoutUploading_IfNotS3OptimisedStrategy()
        {
            // Arrange
            var imageProcessorResponse = new ImageProcessorResponseModel {Thumbs = new ThumbOnDisk[0]};

            var response = httpHandler.GetResponseMessage(JsonConvert.SerializeObject(imageProcessorResponse),
                HttpStatusCode.OK);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpHandler.SetResponse(response);
            
            var context = GetIngestionContext();
            context.Asset.Origin = "https://s3.amazonaws.com/dlcs-storage/2/1/foo-bar";
            context.Asset.InitialOrigin = "https://s3.amazonaws.com/dlcs-storage-ignored/2/1/foo-bar"; 
            context.AssetFromOrigin.CustomerOriginStrategy = new CustomerOriginStrategy
            {
                Optimised = true,
                Strategy = OriginStrategy.S3Ambient
            };

            const string expected = "s3://Fake-Region/dlcs-storage/2/1/foo-bar";

            // Act
            await sut.ProcessImage(context);
            
            // Assert
            bucketReader.ShouldNotHaveKey("0/0/something");
            context.ImageLocation.S3.Should().Be(expected);
        }
        
        [Fact]
        public async Task ProcessImage_ProcessesNewThumbs()
        {
            // Arrange
            var imageProcessorResponse = new ImageProcessorResponseModel
            {
                Thumbs = new[]
                {
                    new ThumbOnDisk {Height = 100, Width = 50, Path = "/path/to/thumb/100.jpg"},
                }
            };

            var response = httpHandler.GetResponseMessage(JsonConvert.SerializeObject(imageProcessorResponse),
                HttpStatusCode.OK);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpHandler.SetResponse(response);
            
            var context = GetIngestionContext();
            context.AssetFromOrigin.CustomerOriginStrategy = new CustomerOriginStrategy {Optimised = false};

            // Act
            await sut.ProcessImage(context);
            
            // Assert
            A.CallTo(() => thumbLayoutManager.CreateNewThumbs(A<Asset>.That.Matches(a => a.Id == context.Asset.Id),
                    A<IEnumerable<ThumbOnDisk>>.That.Matches(t => t.Single().Path.EndsWith("100.jpg")),
                    A<ObjectInBucket>.That.Matches(o => o.ToString() == "s3://thumbs-bucket:::1/2/something/")
                ))
                .MustHaveHappened();
        }
        
        [Fact]
        public async Task ProcessImage_ReturnsImageStorageObject()
        {
            // Arrange
            var imageProcessorResponse = new ImageProcessorResponseModel {Thumbs = new ThumbOnDisk[0]};

            var response = httpHandler.GetResponseMessage(JsonConvert.SerializeObject(imageProcessorResponse),
                HttpStatusCode.OK);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpHandler.SetResponse(response);
            
            var context = GetIngestionContext();
            context.AssetFromOrigin.CustomerOriginStrategy = new CustomerOriginStrategy {Optimised = false};

            // Act
            await sut.ProcessImage(context);
            
            // Assert
            var storage = context.ImageStorage;
            storage.Id.Should().Be("/1/2/something");
            storage.Customer.Should().Be(1);
            storage.Space.Should().Be(2);
            storage.LastChecked.Should().BeCloseTo(DateTime.Now);
            storage.Size.Should().Be(123);
        }
        
        private static IngestionContext GetIngestionContext()
        {
            var asset = new Asset {Id = "/1/2/something", Customer = 1, Space = 2};
            asset
                .WithImageOptimisationPolicy(new ImageOptimisationPolicy {TechnicalDetails = new List<string>()})
                .WithThumbnailPolicy(new ThumbnailPolicy());

            var context = new IngestionContext(asset,
                new AssetFromOrigin("asset-id", 123, "./scratch/here.jpg", "image/jpg"));
            return context;
        }
    }
}