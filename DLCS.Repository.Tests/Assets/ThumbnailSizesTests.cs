using System;
using System.Collections.Generic;
using DLCS.Repository.Assets;
using FluentAssertions;
using IIIF;
using Xunit;

namespace DLCS.Repository.Tests.Assets
{
    public class ThumbnailSizesTests
    {
        [Fact]
        public void JsonCtor_SetsCount()
        {
            // Arrange
            var open = new List<int[]>
            {
                new[] {10, 20},
                new[] {100, 200}
            };

            var auth = new List<int[]> {new[] {400, 800}};

            // Act
            var thumbnailSizes = new ThumbnailSizes(open, auth);

            // Assert
            thumbnailSizes.Count.Should().Be(3);
        }
        
        [Fact]
        public void AddAuth_UpdatesAuthListAndCount()
        {
            // Arrange
            var thumbnailSizes = new ThumbnailSizes();
            var size = new Size(10, 20);
            var expected = new List<int[]> {size.ToArray()};
            thumbnailSizes.AddOpen(new Size(100, 200));
            
            // Act
            thumbnailSizes.AddAuth(size);
            
            // Assert
            thumbnailSizes.Auth.Should().BeEquivalentTo(expected);
            thumbnailSizes.Count.Should().Be(2);
        } 
        
        [Fact]
        public void AddOpen_UpdatesOpenListAndCount()
        {
            // Arrange
            var thumbnailSizes = new ThumbnailSizes();
            var size = new Size(10, 20);
            var expected = new List<int[]> {size.ToArray()};
            thumbnailSizes.AddAuth(new Size(100, 200));
            
            // Act
            thumbnailSizes.AddOpen(size);
            
            // Assert
            thumbnailSizes.Open.Should().BeEquivalentTo(expected);
            thumbnailSizes.Count.Should().Be(2);
        }

        [Fact]
        public void Add_Throws_IfNoMaxSizeSet()
        {
            // Arrange
            var thumbnailSizes = new ThumbnailSizes();
            var open = new Size(80, 80);

            Action action = () => thumbnailSizes.Add(open);

            // Assert
            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Attempt to Add thumb but MaxAvailable has not been set.");
        }

        [Fact]
        public void Add_PutsInCorrectList()
        {
            // Arrange
            var open = new Size(80, 80);
            var auth = new Size(110, 110);
            var thumbnailSizes = new ThumbnailSizes();
            thumbnailSizes.SetMaxAvailableSize(new Size(100, 100));

            // Act
            thumbnailSizes.Add(open);
            thumbnailSizes.Add(auth);
            
            // Assert
            thumbnailSizes.Count.Should().Be(2);
            thumbnailSizes.Auth.Should().BeEquivalentTo(new List<int[]> {auth.ToArray()});
            thumbnailSizes.Open.Should().BeEquivalentTo(new List<int[]> {open.ToArray()});
        }
    }
}