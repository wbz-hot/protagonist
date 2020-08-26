using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Completion;
using Engine.Ingest.Handlers;
using Engine.Ingest.Timebased;
using Engine.Messaging.Models;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Engine.Tests.Ingest.Handlers
{
    public class TranscodeCompleteHandlerTests
    {
        private readonly TranscodeCompleteHandler sut;
        private readonly ITimebasedIngestorCompletion completion;

        public TranscodeCompleteHandlerTests()
        {
            completion = A.Fake<ITimebasedIngestorCompletion>();
            sut = new TranscodeCompleteHandler(completion, new NullLogger<TranscodeCompleteHandler>());
        }

        [Fact]
        public async Task Handle_ReturnsFalse_IfUnableToDeserializeMessage()
        {
            // Arrange
            var queueMessage = new QueueMessage {Body = "medusa"};
            
            // Act
            var result = await sut.Handle(queueMessage, CancellationToken.None);
            
            // Assert
            result.Should().BeFalse();
        }
        
        [Fact]
        public async Task Handle_PassesDeserialisedObject_ToCompleteIngest()
        {
            // Arrange
            const string fileName = "ElasticTranscoderNotification.json";
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples", fileName);

            var queueMessage = new QueueMessage
            {
                Body = await File.ReadAllTextAsync(filePath)
            };
            var cancellationToken = CancellationToken.None;

            // Act
            await sut.Handle(queueMessage, cancellationToken);
            
            // Assert
            A.CallTo(() => completion.CompleteIngestion("2/1/engine_vid_1",
                    A<TranscodeResult>.That.Matches(result =>
                        result.InputKey == "9919/2/1/engine_vid_1" &&
                        result.Outputs.Count == 2 &&
                        result.Outputs[0].Key == "2/1/engine_vid_1/full/full/max/max/0/default.mp4" &&
                        result.Outputs[1].Key == "2/1/engine_vid_1/full/full/max/max/0/default.webm"),
                    cancellationToken))
                .MustHaveHappened();
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_ReturnsResultOfCompleteIngest(bool success)
        {
            // Arrange
            const string fileName = "ElasticTranscoderNotification.json";
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Samples", fileName);

            var queueMessage = new QueueMessage
            {
                Body = await File.ReadAllTextAsync(filePath)
            };
            var cancellationToken = CancellationToken.None;
            
            A.CallTo(() =>
                    completion.CompleteIngestion("2/1/engine_vid_1", A<TranscodeResult>._, cancellationToken))
                .Returns(success);

            // Act
            var result = await sut.Handle(queueMessage, cancellationToken);
            
            // Assert
            result.Should().Be(success);
        }
    }
}