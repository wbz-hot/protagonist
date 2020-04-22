﻿using System;
using DLCS.Model.Assets;
using FluentAssertions;
using Xunit;

namespace DLCS.Model.Tests.Assets
{
    public class AssetTests
    {
        [Theory]
        [InlineData("foo-bar")]
        [InlineData("/foo-bar")]
        [InlineData("2/1/foo-bar")]
        public void GetUniqueName_ReturnsExpected(string id)
        {
            // Arrange
            const string expected = "foo-bar";
            var asset = new Asset {Id = id};

            // Assert
            asset.GetUniqueName().Should().Be(expected);
        }

        [Fact]
        public void MarkAsIngestComplete_SetsFinishedAndIngestingFields()
        {
            // Arrange
            var asset = new Asset {Ingesting = true};
            
            // Act
            asset.MarkAsIngestComplete();
            
            // Assert
            asset.Ingesting.Should().BeFalse();
            asset.Finished.Should().BeCloseTo(DateTime.Now);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void HasError_False_IfErrorHasNoValue(string error)
        {
            // Arrange
            var asset = new Asset {Error = error};
            
            // Assert
            asset.HasError.Should().BeFalse();
        }

        [Fact]
        public void HasError_True_IfErrorHasValue()
        {
            // Arrange
            var asset = new Asset {Error = "foo"};
            
            // Assert
            asset.HasError.Should().BeTrue();
        }
    }
}