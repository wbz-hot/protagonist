using System;
using System.Collections.Generic;
using Engine.Ingest.Models;
using FluentAssertions;
using Xunit;

namespace Engine.Tests.Ingest.Models
{
    public class IncomingIngestEventTests
    {
        [Fact]
        public void AssetJson_Null_IfDictionaryNull()
        {
            // Arrange
            var evt = Create(null);
            
            // Act
            var assetJson = evt.AssetJson;

            // Assert
            assetJson.Should().BeNullOrEmpty();
        }
        
        [Fact]
        public void AssetJson_Null_IfDictionaryEmpty()
        {
            // Arrange
            var evt = Create(new Dictionary<string, string>());
            
            // Act
            var assetJson = evt.AssetJson;

            // Assert
            assetJson.Should().BeNullOrEmpty();
        }
        
        [Fact]
        public void AssetJson_Null_IfDictionaryDoesNotContainCorrectElement()
        {
            // Arrange
            var paramsDict = new Dictionary<string, string>{ ["foo"] = "bar"};
            var evt = Create(paramsDict);
            
            // Act
            var assetJson = evt.AssetJson;

            // Assert
            assetJson.Should().BeNullOrEmpty();
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("something")]
        public void AssetJson_ReturnsExpected_IfDictionaryContainCorrectElement(string value)
        {
            // Arrange
            var paramsDict = new Dictionary<string, string>{ ["image"] = value};
            var evt = Create(paramsDict);
            
            // Act
            var assetJson = evt.AssetJson;

            // Assert
            assetJson.Should().Be(value);
        }

        private IncomingIngestEvent Create(Dictionary<string, string> paramsDict)
            => new IncomingIngestEvent("test", DateTime.Now, "test::type", paramsDict);
    }
}