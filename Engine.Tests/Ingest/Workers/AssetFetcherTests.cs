using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Test.Helpers;
using Engine.Ingest.Strategy;
using Engine.Ingest.Workers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Engine.Tests.Ingest.Workers
{
    public class AssetFetcherTests
    {
        private readonly AssetFetcher sut;
        private readonly ICustomerOriginRepository customerOriginRepository;
        private readonly IOriginStrategy customerOriginStrategy;

        public AssetFetcherTests()
        {
            customerOriginRepository = A.Fake<ICustomerOriginRepository>();
            
            // For unit-test only s3ambient will be mocked
            customerOriginStrategy = A.Fake<IOriginStrategy>();
            A.CallTo(() => customerOriginStrategy.Strategy).Returns(OriginStrategy.S3Ambient);
            var originStrategies = new[] {customerOriginStrategy};

            sut = new AssetFetcher(customerOriginRepository, originStrategies, new NullLogger<AssetFetcher>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void CopyAssetFromOrigin_Throws_IfDestinationFolderNullOrEmpty(string destinationFolder)
        {
            // Act
            Func<Task> action = () => sut.CopyAssetFromOrigin(new Asset(), destinationFolder);
            
            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'destinationFolder')");
        }

        [Theory]
        [InlineData(OriginStrategy.Default)]
        [InlineData(OriginStrategy.BasicHttp)]
        [InlineData(OriginStrategy.SFTP)]
        public void CopyAssetFromOrigin_Throws_CustomerOriginStrategyImplementationNotFound(OriginStrategy strategy)
        {
            // Arrange
            var asset = new Asset();
            var cos = new CustomerOriginStrategy {Strategy = strategy};
            A.CallTo(() => customerOriginRepository.GetCustomerOriginStrategy(asset, true)).Returns(cos);
            
            // Act
            Func<Task> action = () => sut.CopyAssetFromOrigin(asset, "./here");
            
            // Assert
            action.Should().Throw<InvalidOperationException>();
        }
        
        [Fact]
        public void CopyAssetFromOrigin_Throws_IfOriginReturnsNull()
        {
            // Arrange
            var asset = new Asset {Id = "/2/1/godzilla"};
            var cos = new CustomerOriginStrategy {Strategy = OriginStrategy.S3Ambient};
            A.CallTo(() => customerOriginRepository.GetCustomerOriginStrategy(asset, true)).Returns(cos);
            A.CallTo(() => customerOriginStrategy.LoadAssetFromOrigin(asset, cos, A<CancellationToken>._))
                .Returns<OriginResponse>(null);
            
            // Act
            Func<Task> action = () => sut.CopyAssetFromOrigin(asset, "./here");
            
            // Assert
            action.Should().Throw<ApplicationException>();
        }
        
        [Fact]
        public void CopyAssetFromOrigin_Throws_IfOriginReturnsEmptyStream()
        {
            // Arrange
            var asset = new Asset {Id = "/2/1/godzilla"};
            var cos = new CustomerOriginStrategy {Strategy = OriginStrategy.S3Ambient};
            A.CallTo(() => customerOriginRepository.GetCustomerOriginStrategy(asset, true)).Returns(cos);
            A.CallTo(() => customerOriginStrategy.LoadAssetFromOrigin(asset, cos, A<CancellationToken>._))
                .Returns(new OriginResponse(Stream.Null));
            
            // Act
            Func<Task> action = () => sut.CopyAssetFromOrigin(asset, "./here");
            
            // Assert
            action.Should().Throw<ApplicationException>();
        }
        
        [Fact]
        [Trait("Requires", "FileAccess")]
        public async Task CopyAssetFromOrigin_SavesFileToDisk_IfNoContentLength()
        {
            // Arrange
            var c = Path.DirectorySeparatorChar;
            var destination = $".{c}";
            var asset = new Asset {Id = "/2/1/godzilla", Customer = 2, Space = 1};
            var cos = new CustomerOriginStrategy {Strategy = OriginStrategy.S3Ambient};
            A.CallTo(() => customerOriginRepository.GetCustomerOriginStrategy(asset, true)).Returns(cos);
            
            var responseStream = "{\"foo\":\"bar\"}".ToMemoryStream();
            var originResponse = new OriginResponse(responseStream).WithContentType("application/json");
            A.CallTo(() => customerOriginStrategy.LoadAssetFromOrigin(asset, cos, A<CancellationToken>._))
                .Returns(originResponse);
            
            var expectedOutput = $"{destination}2{c}1{c}godzilla";
            
            // Act
            var response = await sut.CopyAssetFromOrigin(asset, destination);
            
            // Assert
            File.Exists(expectedOutput).Should().BeTrue();
            File.Delete(expectedOutput);
            response.LocationOnDisk.Should().Be(expectedOutput);
            response.ContentType.Should().Be("application/json");
            response.AssetSize.Should().BeGreaterThan(0);
            response.AssetId.Should().Be(asset.Id);
            response.CustomerOriginStrategy.Should().Be(cos);
        }
        
        [Fact]
        [Trait("Requires", "FileAccess")]
        public async Task CopyAssetFromOrigin_SavesFileToDisk_IfContentLength()
        {
            // Arrange
            var c = Path.DirectorySeparatorChar;
            var destination = $".{c}";
            var asset = new Asset {Id = "/2/1/godzilla1", Customer = 2, Space = 1};
            var cos = new CustomerOriginStrategy {Strategy = OriginStrategy.S3Ambient};
            A.CallTo(() => customerOriginRepository.GetCustomerOriginStrategy(asset, true)).Returns(cos);
            
            var responseStream = "{\"foo\":\"bar\"}".ToMemoryStream();
            var originResponse = new OriginResponse(responseStream)
                .WithContentType("application/json")
                .WithContentLength(8);
            A.CallTo(() => customerOriginStrategy.LoadAssetFromOrigin(asset, cos, A<CancellationToken>._))
                .Returns(originResponse);
            
            var expectedOutput = $"{destination}2{c}1{c}godzilla1";
            
            // Act
            var response = await sut.CopyAssetFromOrigin(asset, destination);
            
            // Assert
            File.Exists(expectedOutput).Should().BeTrue();
            File.Delete(expectedOutput);
            response.LocationOnDisk.Should().Be(expectedOutput);
            response.ContentType.Should().Be("application/json");
            response.AssetSize.Should().Be(8);
            response.AssetId.Should().Be(asset.Id);
            response.CustomerOriginStrategy.Should().Be(cos);
        }
    }
}