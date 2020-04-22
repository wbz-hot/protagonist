﻿using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using Engine.Ingest.Strategy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Engine.Tests.Ingest.Strategy
{
    public class DefaultOriginStrategyTests
    {
        private readonly DefaultOriginStrategy sut;
        private readonly ControllableHttpMessageHandler httpHandler;

        public DefaultOriginStrategyTests()
        {
            httpHandler = new ControllableHttpMessageHandler();

            var httpClient = new HttpClient(httpHandler);
            sut = new DefaultOriginStrategy(httpClient, new NullLogger<DefaultOriginStrategy>());
        }

        [Fact]
        public async Task LoadAssetFromOrigin_ReturnsExpectedResponse_OnSuccess()
        {
            // Arrange
            var response = httpHandler.GetResponseMessage("this is a test", HttpStatusCode.OK);
            const string contentType = "application/json";
            const long contentLength = 4324;
            
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            response.Content.Headers.ContentLength = contentLength;
            httpHandler.SetResponse(response);

            const string originUri = "https://test.example.com/string";
            
            // Act
            var result = await sut.LoadAssetFromOrigin(new Asset {Origin = originUri}, new CustomerOriginStrategy());
            
            // Assert
            httpHandler.CallsMade.Should().Contain(originUri);
            result.Stream.Should().NotBeNull();
            result.ContentLength.Should().Be(contentLength);
            result.ContentType.Should().Be(contentType);
        }
        
        [Fact]
        public async Task LoadAssetFromOrigin_UsesInitialOrigin_IfSpecified()
        {
            // Arrange
            var response = httpHandler.GetResponseMessage("", HttpStatusCode.OK);
            httpHandler.SetResponse(response);
            const string originUri = "https://test.example.com/string";
            const string initialOrigin = "https://initial.origin.com";
            
            // Act
            await sut.LoadAssetFromOrigin(new Asset {Origin = originUri, InitialOrigin = initialOrigin},
                new CustomerOriginStrategy());
            
            // Assert
            httpHandler.CallsMade.Should().ContainSingle(initialOrigin);
        }
        
        [Fact]
        public async Task LoadAssetFromOrigin_HandlesNoContentLengthAndType()
        {
            // Arrange
            var response = httpHandler.GetResponseMessage("", HttpStatusCode.OK);
            httpHandler.SetResponse(response);
            const string originUri = "https://test.example.com/string";
            
            // Act
            var result = await sut.LoadAssetFromOrigin(new Asset {Origin = originUri}, new CustomerOriginStrategy());
            
            // Assert
            httpHandler.CallsMade.Should().Contain(originUri);
            result.Stream.Should().NotBeNull();
            result.ContentLength.Should().BeNull();
            result.ContentType.Should().BeNull();
        }
        
        [Theory]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task LoadAssetFromOrigin_ReturnsNull_IfCallFails(HttpStatusCode statusCode)
        {
            // Arrange
            var response = httpHandler.GetResponseMessage("uh-oh", statusCode);
            httpHandler.SetResponse(response);
            const string originUri = "https://test.example.com/string";
            
            // Act
            var result = await sut.LoadAssetFromOrigin(new Asset {Origin = originUri}, new CustomerOriginStrategy());
            
            // Assert
            httpHandler.CallsMade.Should().Contain(originUri);
            result.Should().BeNull();
        }
    }
}