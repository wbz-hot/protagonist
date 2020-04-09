using System;
using System.Collections.Generic;
using DLCS.Core.Reflection;
using FluentAssertions;
using Xunit;

namespace DLCS.Core.Tests.Reflection
{
    public class TypeXTests
    {
        [Fact]
        public void IsEnumerable_False_IfTypeIsNull()
        {
            // Arrange
            Type t = null;

            // Assert
            t.IsEnumerable().Should().BeFalse();
        }
        
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(DateTime))]
        public void IsEnumerable_False_IfTypeIsNotEnumerable(Type t) 
            => t.IsEnumerable().Should().BeFalse();
        
        [Theory]
        [InlineData(typeof(Array))]
        [InlineData(typeof(IEnumerable<string>))]
        [InlineData(typeof(List<string>))]
        [InlineData(typeof(string[]))]
        public void IsEnumerable_True_IfTypeIsEnumerable(Type t) 
            => t.IsEnumerable().Should().BeTrue();
    }
}