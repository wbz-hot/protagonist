using FluentAssertions;
using Xunit;

namespace DLCS.Core.Tests
{
    public class MIMEHelperTests
    {
        [Theory]
        [InlineData("application/pdf", "pdf")]
        [InlineData("image/svg+xml", "svg")]
        [InlineData("image/jpg", "jpg")]
        [InlineData("image/jpg;foo=bar", "jpg")]
        public void GetExtensionForContentType_CorrectForKnownTypes(string contentType, string expected) 
            => MIMEHelper.GetExtensionForContentType(contentType).Should().Be(expected);

        [Fact]
        public void GetExtensionForContentType_ReturnsNullForUnknownTypes()
            => MIMEHelper.GetExtensionForContentType("foo/bar").Should().BeNull();
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GetExtensionForContentType_ReturnsNullForNullOrWhitespace(string contentType)
            => MIMEHelper.GetExtensionForContentType(contentType).Should().BeNull();
    }
}