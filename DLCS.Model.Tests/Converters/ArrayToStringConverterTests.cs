using System;
using System.Collections.Generic;
using DLCS.Core.Reflection;
using DLCS.Model.Converters;
using FluentAssertions;
using Xunit;

namespace DLCS.Model.Tests.Converters
{
    public class ArrayToStringConverterTests
    {
        private readonly ArrayToStringConverter sut;

        public ArrayToStringConverterTests()
        {
            sut = new ArrayToStringConverter();
        }

        [Theory]
        [InlineData(typeof(Array))]
        [InlineData(typeof(IEnumerable<string>))]
        [InlineData(typeof(List<string>))]
        [InlineData(typeof(string[]))]
        public void CanConvert_True_IfEnumerable(Type t)
            => t.IsEnumerable().Should().BeTrue();


    }
}