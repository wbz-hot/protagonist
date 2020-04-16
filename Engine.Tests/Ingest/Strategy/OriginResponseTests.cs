using System;
using System.IO;
using Engine.Ingest.Strategy;
using FluentAssertions;
using Xunit;

namespace Engine.Tests.Ingest.Strategy
{
    public class OriginResponseTests
    {
        [Fact]
        public void Ctor_Throws_IfStreamNull()
        {
            // Arrange
            Action action = () => new OriginResponse(null);
            
            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'stream')");
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData(-1)]
        [InlineData(0)]
        public void WithContentLength_DoesNotSetContentLength_IfNullOrLessThan1(long? contentLength)
        {
            // Arrange
            var response = new OriginResponse(Stream.Null);
            
            // Act
            response.WithContentLength(contentLength);
            
            // Assert
            response.ContentLength.Should().BeNull();
        }
    }
}