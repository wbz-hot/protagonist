﻿using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Security;
using DLCS.Test.Helpers.Web;
using Engine.Ingest.Strategy;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Engine.Tests.Ingest.Strategy
{
    public class BasicHttpAuthOriginStrategyTests
    {
        private readonly BasicHttpAuthOriginStrategy sut;
        private readonly ControllableHttpMessageHandler httpHandler;
        private readonly ICredentialsRepository credentialsRepository;
        private readonly CustomerOriginStrategy customerOriginStrategy;

        public BasicHttpAuthOriginStrategyTests()
        {
            credentialsRepository = A.Fake<ICredentialsRepository>();
            httpHandler = new ControllableHttpMessageHandler();

            var httpClient = new HttpClient(httpHandler);
            customerOriginStrategy = new CustomerOriginStrategy {Strategy = OriginStrategy.BasicHttp};

            sut = new BasicHttpAuthOriginStrategy(httpClient, credentialsRepository,
                new NullLogger<BasicHttpAuthOriginStrategy>());
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
            
            var basicCreds = new BasicCredentials {User = "user", Password = "password"};
            const string expectedAuthHeader = "Basic dXNlcjpwYXNzd29yZA==";
            A.CallTo(() => credentialsRepository.GetBasicCredentialsForOriginStrategy(A<CustomerOriginStrategy>._))
                .Returns(basicCreds);
            
            const string originUri = "https://test.example.com/string";
            
            string actualAuthHeader = null;
            httpHandler.RegisterCallback(message => actualAuthHeader = message.Headers.Authorization.ToString());
            
            // Act
            var result = await sut.LoadAssetFromOrigin(new Asset {Origin = originUri}, customerOriginStrategy);
            
            // Assert
            httpHandler.CallsMade.Should().Contain(originUri);
            actualAuthHeader.Should().Be(expectedAuthHeader);
            result.Stream.Should().NotBeNull();
            result.ContentLength.Should().Be(contentLength);
            result.ContentType.Should().Be(contentType);
        }
        
        [Fact]
        public async Task LoadAssetFromOrigin_LoadAssetFromOrigin_IfSpecified()
        {
            // Arrange
            var response = httpHandler.GetResponseMessage("this is a test", HttpStatusCode.OK);
            httpHandler.SetResponse(response);
            
            A.CallTo(() => credentialsRepository.GetBasicCredentialsForOriginStrategy(A<CustomerOriginStrategy>._))
                .Returns(new BasicCredentials {User = "user", Password = "password"});
            
            const string originUri = "https://test.example.com/string";
            const string initialOrigin = "https://initial.origin.com";
            
            // Act
            await sut.LoadAssetFromOrigin(new Asset {Origin = originUri, InitialOrigin = initialOrigin},
                customerOriginStrategy);
            
            // Assert
            httpHandler.CallsMade.Should().ContainSingle(initialOrigin);
        }
        
        [Fact]
        public async Task LoadAssetFromOrigin_HandlesNoContentLengthAndType()
        {
            // Arrange
            var response = httpHandler.GetResponseMessage("", HttpStatusCode.OK);
            httpHandler.SetResponse(response);
            
            A.CallTo(() => credentialsRepository.GetBasicCredentialsForOriginStrategy(A<CustomerOriginStrategy>._))
                .Returns(new BasicCredentials {User = "user", Password = "password"});
            
            const string originUri = "https://test.example.com/string";
            
            // Act
            var result = await sut.LoadAssetFromOrigin(new Asset {Origin = originUri},
                customerOriginStrategy);
            
            // Assert
            httpHandler.CallsMade.Should().Contain(originUri);
            result.Stream.Should().NotBeNull();
            result.ContentLength.Should().BeNull();
            result.ContentType.Should().Be("text/plain");
        }
        
        [Fact]
        public async Task LoadAssetFromOrigin_ReturnsNull_IfNoCredentialsFound()
        {
            // Arrange
            var response = httpHandler.GetResponseMessage("", HttpStatusCode.OK);
            httpHandler.SetResponse(response);
            A.CallTo(() => credentialsRepository.GetBasicCredentialsForOriginStrategy(A<CustomerOriginStrategy>._))
                .Returns<BasicCredentials>(null);
            
            const string originUri = "https://test.example.com/string";
            
            // Act
            var result = await sut.LoadAssetFromOrigin(new Asset {Origin = originUri},
                customerOriginStrategy);
            
            // Assert
            httpHandler.CallsMade.Should().BeNullOrEmpty();
            result.Should().BeNull();
        }
        
        [Theory]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task LoadAssetFromOrigin_ReturnsNull_IfCallFails(HttpStatusCode statusCode)
        {
            // Arrange
            var response = httpHandler.GetResponseMessage("", statusCode);
            httpHandler.SetResponse(response);
            A.CallTo(() => credentialsRepository.GetBasicCredentialsForOriginStrategy(A<CustomerOriginStrategy>._))
                .Returns(new BasicCredentials {User = "user", Password = "password"});
            
            const string originUri = "https://test.example.com/string";
            
            // Act
            var result = await sut.LoadAssetFromOrigin(new Asset {Origin = originUri},
                customerOriginStrategy);
            
            // Assert
            httpHandler.CallsMade.Should().Contain(originUri);
            result.Should().BeNull();
        }
    }
}