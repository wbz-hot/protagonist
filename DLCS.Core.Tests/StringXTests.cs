using FluentAssertions;
using Xunit;

namespace DLCS.Core.Tests
{
    public class StringXTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void SplitCsvString_ReturnsEmptyList_IfNullOrEmpty(string str)
            => str.SplitCsvString().Should().BeEmpty();

        [Fact]
        public void SplitCsvString_SplitsStringCorrectly()
        {
            // Arrange
            const string original = "foo,bar,,baz";
            var expected = new[] {"foo", "bar", "baz"};
            
            // Act
            var actual = original.SplitCsvString();
            
            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void SplitCsvString_Convert_ReturnsEmptyList_IfNullOrEmpty(string str)
            => str.SplitCsvString(int.Parse).Should().BeEmpty();

        [Fact]
        public void SplitCsvString_Convert_SplitsStringCorrectly()
        {
            // Arrange
            const string original = "1,2,,3";
            var expected = new[] {1, 2, 3};
            
            // Act
            var actual = original.SplitCsvString(int.Parse);
            
            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
    }
}