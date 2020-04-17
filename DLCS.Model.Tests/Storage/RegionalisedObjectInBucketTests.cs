using DLCS.Model.Storage;
using FluentAssertions;
using Xunit;

namespace DLCS.Model.Tests.Storage
{
    public class RegionalisedObjectInBucketTests
    {
        [Fact]
        public void Parse_Correct_S3Qualified()
        {
            // Arrange
            const string uri = "s3://eu-west-1/dlcs-storage/2/1/foo-bar";
            var expected = new RegionalisedObjectInBucket
            {
                Bucket = "dlcs-storage",
                Key = "2/1/foo-bar",
                Region = "eu-west-1"
            };

            // Act
            var actual = RegionalisedObjectInBucket.Parse(uri);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Theory]
        [InlineData("http://s3-eu-west-1.amazonaws.com/dlcs-storage/2/1/foo-bar")]
        [InlineData("https://s3.eu-west-1.amazonaws.com/dlcs-storage/2/1/foo-bar")]
        public void Parse_Correct_Http1(string uri)
        {
            // Arrange
            var expected = new RegionalisedObjectInBucket
            {
                Bucket = "dlcs-storage",
                Key = "2/1/foo-bar",
                Region = "eu-west-1"
            };

            // Act
            var actual = RegionalisedObjectInBucket.Parse(uri);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Theory]
        [InlineData("http://dlcs-storage.s3.amazonaws.com/2/1/foo-bar")]
        [InlineData("https://dlcs-storage.s3.amazonaws.com/2/1/foo-bar")]
        public void Parse_Correct_Http2(string uri)
        {
            // Arrange
            var expected = new RegionalisedObjectInBucket
            {
                Bucket = "dlcs-storage",
                Key = "2/1/foo-bar",
            };

            // Act
            var actual = RegionalisedObjectInBucket.Parse(uri);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Theory]
        [InlineData("http://dlcs-storage.s3.eu-west-1.amazonaws.com/2/1/foo-bar")]
        [InlineData("https://dlcs-storage.s3.eu-west-1.amazonaws.com/2/1/foo-bar")]
        public void Parse_Correct_Http3(string uri)
        {
            // Arrange
            var expected = new RegionalisedObjectInBucket
            {
                Bucket = "dlcs-storage",
                Key = "2/1/foo-bar",
                Region = "eu-west-1"
            };

            // Act
            var actual = RegionalisedObjectInBucket.Parse(uri);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Theory]
        [InlineData("http://s3.amazonaws.com/dlcs-storage/2/1/foo-bar")]
        [InlineData("https://s3.amazonaws.com/dlcs-storage/2/1/foo-bar")]
        public void Parse_Correct_Http4(string uri)
        {
            // Arrange
            var expected = new RegionalisedObjectInBucket
            {
                Bucket = "dlcs-storage",
                Key = "2/1/foo-bar",
            };

            // Act
            var actual = RegionalisedObjectInBucket.Parse(uri);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void Parse_Null_IfNoMatches()
        {
            // Arrange
            const string uri = "http://example.org";

            // Act
            var actual = RegionalisedObjectInBucket.Parse(uri);

            // Assert
            actual.Should().BeNull();
        }
    }
}