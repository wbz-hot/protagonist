using System;
using System.Collections.Generic;
using DLCS.Core.Guard;
using FluentAssertions;
using Xunit;

namespace DLCS.Core.Tests.Guard
{
    public class GuardXTests
    {
        [Fact]
        public void ThrowIfNull_Throws_IfArgumentNull()
        {
            // Act
            Action action = () => GuardX.ThrowIfNull<object>(null, "foo");
            
            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'foo')");
        } 
        
        [Fact]
        public void ThrowIfNull_ReturnsProvidedValue_IfNotNull()
        {
            // Arrange
            object val = DateTime.Now;
            
            // Act
            var actual = val.ThrowIfNull(nameof(val));
            
            // Assert
            actual.Should().Be(val);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ThrowIfNullOrWhiteSpace_Throws_IfArgumentNullOrWhiteSpace(string str)
        {
            // Act
            Action action = () => str.ThrowIfNullOrWhiteSpace("foo");
            
            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'foo')");
        }
        
        [Fact]
        public void ThrowIfNullOrWhiteSpace_ReturnsProvidedString_IfNotNull()
        {
            // Arrange
            const string val = "hi";

            // Act
            var actual = val.ThrowIfNull(nameof(val));
            
            // Assert
            actual.Should().Be(val);
        }

        [Fact]
        public void ThrowIfNullOrEmpty_List_ThrowsIfNull()
        {
            // Arrange
            int[] arg = null;
            
            // Act
            Action action = () => arg.ThrowIfNullOrEmpty("foo");
            
            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'foo')");
        }
        
        [Fact]
        public void ThrowIfNullOrEmpty_List_ThrowsIfEmpty()
        {
            // Arrange
            var arg = new List<int>();
            
            // Act
            Action action = () => arg.ThrowIfNullOrEmpty("foo");
            
            // Assert
            action.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'foo')");
        }
        
        [Fact]
        public void ThrowIfNullOrEmpty_List_ReturnsProvidedList_IfHasValues()
        {
            // Arrange
            var arg = new List<int> {3};
            
            // Act
            var actual = arg.ThrowIfNullOrEmpty("foo");
            
            // Assert
            actual.Should().BeEquivalentTo(arg);
        }
    }
}
