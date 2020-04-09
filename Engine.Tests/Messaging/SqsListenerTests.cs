using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using Engine.Messaging;
using Engine.Messaging.Models;
using FakeItEasy;
using FizzWare.NBuilder;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Engine.Tests.Messaging
{
    public class SqsListenerTests
    {
        private readonly IMessageHandler messageHandler;
        private readonly IAmazonSQS sqsClient;
        private readonly SqsListener sut;
        private readonly SubscribedToQueue subscribedQueue;

        public SqsListenerTests()
        {
            messageHandler = A.Fake<IMessageHandler>();
            sqsClient = A.Fake<IAmazonSQS>();
            subscribedQueue = new SubscribedToQueue("test-queue");
            subscribedQueue.SetUri("https://queues.com/test-queue");

            sut = new SqsListener(sqsClient, subscribedQueue, queue => messageHandler, new NullLoggerFactory());
        }

        [Fact]
        public void Listen_SetsIsListeningTrue()
        {
            // Act
            sut.Listen(CancellationToken.None);
            
            // Assert
            sut.IsListening.Should().BeTrue();
        }
        
        [Fact(Skip = "Kicks off background thread which needs some thought on testing.")]
        public void Listen_CallsHandlerForMessagesInQueue()
        {
            var cts = new CancellationTokenSource();
            var messages = Builder<Message>.CreateListOfSize(2).Build().ToList();
            var receiveMessageResponse = Builder<ReceiveMessageResponse>
                .CreateNew()
                .With(b => b.Messages = messages)
                .Build();
            
            // Arrange
            A.CallTo(() =>
                    sqsClient.ReceiveMessageAsync(
                        A<ReceiveMessageRequest>.That.Matches(r => r.QueueUrl == subscribedQueue.Url),
                        A<CancellationToken>._))
                .Returns(receiveMessageResponse);

            A.CallTo(() => messageHandler.Handle(A<QueueMessage>._, A<CancellationToken>._))
                .Invokes(() => cts.Cancel());
            
            // Act
            sut.Listen(cts.Token);
            
            // Assert
            A.CallTo(() => messageHandler.Handle(
                    A<QueueMessage>.That.Matches(qm => qm.Body == messages[0].Body),
                    A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }
    }
}