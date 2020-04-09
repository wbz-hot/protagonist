using System;
using System.Collections;
using System.Collections.Generic;
using Engine.Ingest.Models;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Engine.Tests.Ingest.Models
{
    public class IngestRequestConverterTests
    {
        [Fact]
        public void ConvertToInternalRequest_Throws_IfIncomingRequestNull()
        {
            // Arrange
            IncomingIngestEvent request = null;
            
            // Act
            Action action = () => request.ConvertToInternalRequest();
            
            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'incomingRequest')");
        }
        
        [Fact]
        public void ConvertToInternalRequest_Throws_IfIncomingRequestDoesNotContainAssetJson()
        {
            // Arrange
            var request = Create(new Dictionary<string, string>());
            
            // Act
            Action action = () => request.ConvertToInternalRequest();
            
            // Assert
            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Cannot convert IncomingIngestEvent that has no Asset Json");
        }
        
        [Fact]
        public void ConvertToInternalRequest_Throws_IfIncomingRequestContainsAssetJson_InInvalidFormat()
        {
            // Arrange
            const string assetJson = "i-am-not-json{}";
            var paramsDict = new Dictionary<string, string>{ ["image"] = assetJson};
            var request = Create(paramsDict);
            
            // Act
            Action action = () => request.ConvertToInternalRequest();
            
            // Assert
            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Unable to deserialize Asset Json from IncomingIngestEvent")
                .And.Data.Should().Contain(new DictionaryEntry {Key = "AssetJson", Value = assetJson});
        }

        [Theory]
        [InlineData("{\"id\": \"2/1/engine-9\",\"customer\": 2,\"space\": 1,\"rawId\": \"engine-9\",\"created\": \"2020-04-09T00:00:00+00:00\",\"origin\": \"https://burst.shopifycdn.com/photos/chrome-engine-close-up.jpg\",\"tags\": [],\"roles\": [\"https://api.dlcs.digirati.io/customers/2/roles/clickthrough\"  ],  \"preservedUri\": \"\",  \"string1\": \"\",  \"string2\": \"\",  \"string3\": \"\",  \"maxUnauthorised\": 300,  \"number1\": 0,  \"number2\": 0,  \"number3\": 0,  \"width\": 0,  \"height\": 0,  \"duration\": 0,\"error\": \"\",\"batch\": 0,\"finished\": null,\"ingesting\": false,\"imageOptimisationPolicy\": \"fast-higher\",\"thumbnailPolicy\": \"default\",\"family\": \"I\",\"mediaType\": \"image/jp2\"}")]
        [InlineData("{\r\n  \"id\": \"2/1/engine-9\",\r\n  \"customer\": 2,\r\n  \"space\": 1,\r\n  \"rawId\": \"engine-9\",\r\n  \"created\": \"2020-04-09T00:00:00+00:00\",\r\n  \"origin\": \"https://burst.shopifycdn.com/photos/chrome-engine-close-up.jpg\",\r\n  \"tags\": [],\r\n  \"roles\": [\r\n    \"https://api.dlcs.digirati.io/customers/2/roles/clickthrough\"\r\n  ],\r\n  \"preservedUri\": \"\",\r\n  \"string1\": \"\",\r\n  \"string2\": \"\",\r\n  \"string3\": \"\",\r\n  \"maxUnauthorised\": 300,\r\n  \"number1\": 0,\r\n  \"number2\": 0,\r\n  \"number3\": 0,\r\n  \"width\": 0,\r\n  \"height\": 0,\r\n  \"duration\": 0,\r\n  \"error\": \"\",\r\n  \"batch\": 0,\r\n  \"finished\": null,\r\n  \"ingesting\": false,\r\n  \"imageOptimisationPolicy\": \"fast-higher\",\r\n  \"thumbnailPolicy\": \"default\",\r\n  \"family\": \"I\",\r\n  \"mediaType\": \"image/jp2\"\r\n}")]
        public void ConvertToInternalRequest_ReturnsExpected(string assetJson)
        {
            // Arrange
            var paramsDict = new Dictionary<string, string>{ ["image"] = assetJson};
            var request = Create(paramsDict);
            
            // Act
            var result = request.ConvertToInternalRequest();

            // Assert
            // NOTE - not testing conversion, just ensuring we get something back
            result.Asset.Id.Should().Be("2/1/engine-9");
            result.Created.Should().BeCloseTo(DateTime.Now, 2000);
        }

        private IncomingIngestEvent Create(Dictionary<string, string> paramsDict)
            => new IncomingIngestEvent("test", DateTime.Now, "test::type", paramsDict);
    }
}