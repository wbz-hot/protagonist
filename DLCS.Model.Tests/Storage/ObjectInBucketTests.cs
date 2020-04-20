using System;
using DLCS.Model.Storage;
using FluentAssertions;
using Xunit;

namespace DLCS.Model.Tests.Storage
{
    public class ObjectInBucketTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Ctor_Throws_IfBucketNullOrWhitespace(string bucket)
        {
            // Arrange
            Action action = () => new ObjectInBucket(bucket);
            
            // Assert
            action.Should().Throw<ArgumentNullException>();
        }
    }
}