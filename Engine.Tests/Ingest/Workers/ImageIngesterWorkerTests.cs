using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Policies;
using DLCS.Test.Helpers.Settings;
using Engine.Ingest;
using Engine.Ingest.Completion;
using Engine.Ingest.Image;
using Engine.Ingest.Models;
using Engine.Ingest.Workers;
using Engine.Settings;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Engine.Tests.Ingest.Workers
{
    [Trait("Requires", "FileAccess")]
    public class ImageIngesterWorkerTests
    {
        private readonly IAssetFetcher assetFetcher;
        private readonly IOptionsMonitor<EngineSettings> engineOptionsMonitor;
        private readonly IIngestorCompletion ingestorCompletion;
        private readonly FakeImageProcessor imageProcessor;
        private readonly IPolicyRepository policyRepository;
        private readonly ILogger<ImageIngesterWorker> logger;
        private readonly ImageIngesterWorker sut;

        public ImageIngesterWorkerTests()
        {
            var c = Path.DirectorySeparatorChar;
            var engineSettings = new EngineSettings
            {
                ProcessingFolder = $".{c}here{c}",
                S3Template = "s3://eu-west-1/storage-bucket/{0}/{1}/{2}",
                ScratchRoot = $".{c}scratch{c}",
                ImageIngest = new ImageIngestSettings
                {
                    DestinationTemplate = $"{{root}}{{customer}}{c}{{space}}{c}{{image}}{c}output{c}",
                    SourceTemplate = $"{{root}}{{customer}}{c}{{space}}{c}{{image}}{c}",
                    ThumbsTemplate = $"{{root}}{{customer}}{c}{{space}}{c}{{image}}{c}output{c}thumb{c}",
                }
            };
            var optionsMonitor = OptionsHelpers.GetOptionsMonitor(engineSettings);

            assetFetcher = A.Fake<IAssetFetcher>();
            ingestorCompletion = A.Fake<IIngestorCompletion>();
            imageProcessor = new FakeImageProcessor();
            policyRepository = A.Fake<IPolicyRepository>();
            
            sut = new ImageIngesterWorker(imageProcessor, assetFetcher, policyRepository,optionsMonitor,
                ingestorCompletion, new NullLogger<ImageIngesterWorker>());
        }

        [Fact]
        public async Task Ingest_ReturnsFailed_IfFetcherFailed()
        {
            // Arrange
            A.CallTo(() => assetFetcher.CopyAssetToDisk(A<Asset>._, A<string>._, A<CancellationToken>._))
                .ThrowsAsync(new ArgumentNullException());
            
            // Act
            var result = await sut.Ingest(new IngestAssetRequest(new Asset(), new DateTime()));
            
            // Assert
            result.Should().Be(IngestResult.Failed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]        
        public async Task Ingest_CompletesIngestion_RegardlessOfImageProcessResult(bool imageProcessSuccess)
        {
            // Arrange
            var target = $".{Path.PathSeparator}{nameof(Ingest_CompletesIngestion_RegardlessOfImageProcessResult)}";

            try
            {
                var asset = new Asset {Id = "/2/1/remurdered", Customer = 2, Space = 1};
                File.WriteAllText(target, "{\"foo\":\"bar\"}");

                A.CallTo(() => assetFetcher.CopyAssetToDisk(A<Asset>._, A<string>._, A<CancellationToken>._))
                    .Returns(new AssetFromOrigin(asset.Id, 13, target, "application/json"));
                imageProcessor.ReturnValue = imageProcessSuccess;

                // Act
                await sut.Ingest(new IngestAssetRequest(asset, new DateTime()));
                
                // Assert
                A.CallTo(() => ingestorCompletion.CompleteIngestion(A<IngestionContext>._, imageProcessSuccess))
                    .MustHaveHappened();
                imageProcessor.WasCalled.Should().BeTrue();
            }
            finally
            {
                // Cleanup
                File.Delete(target);
            }
        }

        [Theory]
        [InlineData(true, true, IngestResult.Success)]
        [InlineData(false, true, IngestResult.Failed)]
        [InlineData(true, false, IngestResult.Failed)]
        public async Task Ingest_ReturnsCorrectResult_DependingOnIngestAndCompletion(bool imageProcessSuccess,
            bool completeResult, IngestResult expected)
        {
            // Arrange
            var target = $".{Path.PathSeparator}{nameof(Ingest_ReturnsCorrectResult_DependingOnIngestAndCompletion)}";

            try
            {
                var asset = new Asset {Id = "/2/1/remurdered", Customer = 2, Space = 1};
                File.WriteAllText(target, "{\"foo\":\"bar\"}");

                A.CallTo(() => assetFetcher.CopyAssetToDisk(A<Asset>._, A<string>._, A<CancellationToken>._))
                    .Returns(new AssetFromOrigin(asset.Id, 13, target, "application/json"));

                A.CallTo(() => ingestorCompletion.CompleteIngestion(A<IngestionContext>._, imageProcessSuccess))
                    .Returns(completeResult);

                imageProcessor.ReturnValue = imageProcessSuccess;

                // Act
                var result = await sut.Ingest(new IngestAssetRequest(asset, new DateTime()));

                // Assert
                result.Should().Be(expected);
            }
            finally
            {
                // Cleanup
                File.Delete(target);
            }
        }

        public class FakeImageProcessor : IImageProcessor
        {
            public bool WasCalled { get; private set; }

            public bool ReturnValue { get; set; }
            
            public Action<IngestionContext> Callback { get; set; }
            
            public Task<bool> ProcessImage(IngestionContext context)
            {
                WasCalled = true;
                
                Callback?.Invoke(context);

                return Task.FromResult(ReturnValue);
            }
        }
    }
}