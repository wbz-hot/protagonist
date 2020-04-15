using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using Engine.Ingest.Strategy;
using FluentAssertions;
using Xunit;

namespace Engine.Tests.Ingest.Strategy
{
    public class SafetyCheckOriginStrategyTests
    {
        private readonly TestStrategy sut;

        public SafetyCheckOriginStrategyTests()
        {
            sut = new TestStrategy();
        }
        
        [Fact]
        public void LoadAssetFromOrigin_Throws_IfCustomerOriginStrategyNull()
        {
            // Act
            Action action = () => sut.LoadAssetFromOrigin(new Asset(), null);
            
            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'customerOriginStrategy')");
        }
        
        [Theory]
        [InlineData(OriginStrategy.Default)]
        [InlineData(OriginStrategy.BasicHttp)]
        [InlineData(OriginStrategy.SFTP)]
        public void LoadAssetFromOrigin_Throws_IfCustomerOriginStrategyDiffersFromImplementationStrategy(OriginStrategy strategy)
        {
            // Arrange
            var customerOriginStrategy = new CustomerOriginStrategy {Strategy = strategy};
            
            // Act
            Action action = () => sut.LoadAssetFromOrigin(new Asset(), customerOriginStrategy);
            
            // Assert
            action.Should()
                .Throw<InvalidOperationException>();
        }
        
        [Fact]
        public void LoadAssetFromOrigin_Throws_IfAssetNull()
        {
            // Arrange
            var customerOriginStrategy = new CustomerOriginStrategy {Strategy = OriginStrategy.S3Ambient};
            
            // Act
            Action action = () => sut.LoadAssetFromOrigin(null, customerOriginStrategy);
            
            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'asset')");
        }
        
        [Fact]
        public async Task LoadAssetFromOrigin_CallsImplementation_IfAssetNull()
        {
            // Arrange
            var customerOriginStrategy = new CustomerOriginStrategy {Strategy = OriginStrategy.S3Ambient};
            
            // Act
            await sut.LoadAssetFromOrigin(new Asset(), customerOriginStrategy);
            
            // Assert
            sut.HaveBeenCalled.Should().BeTrue();
        }
        
        private class TestStrategy : SafetyCheckOriginStrategy
        {
            public override OriginStrategy Strategy => OriginStrategy.S3Ambient;
            
            public bool HaveBeenCalled { get; private set; }

            protected override Task<Stream> LoadAssetFromOriginImpl(Asset asset,
                CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
            {
                HaveBeenCalled = true;
                var bytes = Encoding.ASCII.GetBytes("foobar");
                return Task.FromResult<Stream>(new MemoryStream(bytes));
            }
        }
    }
}