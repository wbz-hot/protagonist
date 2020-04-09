using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest;
using Engine.Ingest.Models;
using Engine.Ingest.Workers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Engine.Tests.Ingest
{
    public class IngestControllerTests
    {
        private readonly IngestController sut;
        private readonly IAssetIngesterWorker assetIngesterWorker;
        private readonly IncomingIngestEvent message;

        public IngestControllerTests()
        {
            assetIngesterWorker = A.Fake<IAssetIngesterWorker>();
            sut = new IngestController(
                new AssetIngester(family => assetIngesterWorker, new NullLogger<AssetIngester>()));

            message = new IncomingIngestEvent("test", DateTime.Now, "test",
                new Dictionary<string, string> {["image"] = "{\"id\": \"2/1/engine-9\"}"});
        }

        [Fact]
        public async Task IngestImage_ReturnsOk_IfIngestSuccess()
        {
            // Arrange
            A.CallTo(() => assetIngesterWorker.Ingest(A<IngestAssetRequest>._, CancellationToken.None))
                .Returns(IngestResult.Success);
            
            // Act
            var result = await sut.IngestImage(message, CancellationToken.None);
            
            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
        
        [Fact]
        public async Task IngestImage_ReturnsAccepted_IfIngestQueued()
        {
            // Arrange
            A.CallTo(() => assetIngesterWorker.Ingest(A<IngestAssetRequest>._, CancellationToken.None))
                .Returns(IngestResult.QueuedForProcessing);
            
            // Act
            var result = await sut.IngestImage(message, CancellationToken.None);
            
            // Assert
            result.Should().BeOfType<AcceptedResult>();
        }
        
        [Theory]
        [InlineData(IngestResult.Failed)]
        [InlineData(IngestResult.Unknown)]
        public async Task IngestImage_Returns500_IfNotSuccess(IngestResult ingestResult)
        {
            // Arrange
            A.CallTo(() => assetIngesterWorker.Ingest(A<IngestAssetRequest>._, CancellationToken.None))
                .Returns(ingestResult);
            
            // Act
            var result = await sut.IngestImage(message, CancellationToken.None);
            
            // Assert
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }
    }
}