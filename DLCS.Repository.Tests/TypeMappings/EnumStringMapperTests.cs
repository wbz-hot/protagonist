using System.ComponentModel;
using System.Data;
using DLCS.Repository.TypeMappings;
using FluentAssertions;
using Xunit;

namespace DLCS.Repository.Tests.TypeMappings
{
    public class EnumStringMapperTests
    {
        private readonly EnumStringMapper<TestEnum> sut;

        public EnumStringMapperTests()
        {
            sut = new EnumStringMapper<TestEnum>();
        }
        
        [Fact]
        public void SetValue_SetsValueNull_IfGivenNull()
        {
            // Arrange
            var dataParameter = new TestDbDataParameter {DbType = DbType.Binary};
            
            // Act
            sut.SetValue(dataParameter, null);
            
            // Assert
            dataParameter.DbType.Should().Be(DbType.String);
            dataParameter.Value.Should().BeNull();
        }
        
        [Theory]
        [InlineData(TestEnum.One, "One")]
        [InlineData(TestEnum.Two, "num-two")]
        public void SetValue_SetsValue_BasedOnEnumOrDescription(TestEnum value, string expected)
        {
            // Arrange
            var dataParameter = new TestDbDataParameter {DbType = DbType.Binary};

            // Act
            sut.SetValue(dataParameter, value);
            
            // Assert
            dataParameter.DbType.Should().Be(DbType.String);
            dataParameter.Value.Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Parse_ReturnsNull_IfValueIsNullOrWhitespace(string value)
        {
            // Act
            var actual = sut.Parse(typeof(TestEnum), value);
            
            // Assert
            actual.Should().BeNull();
        }
        
        [Theory]
        [InlineData("One", TestEnum.One)]
        [InlineData("num-two", TestEnum.Two)]
        public void Parse_ReturnsCorrectValue(string value, TestEnum expected)
        {
            // Act
            var actual = sut.Parse(typeof(TestEnum), value);
            
            // Assert
            actual.Should().Be(expected);
        }

        private class TestDbDataParameter : IDbDataParameter
        {
            public DbType DbType { get; set; }
            public ParameterDirection Direction { get; set; }
            public bool IsNullable { get; }
            public string ParameterName { get; set; }
            public string SourceColumn { get; set; }
            public DataRowVersion SourceVersion { get; set; }
            public object Value { get; set; }
            public byte Precision { get; set; }
            public byte Scale { get; set; }
            public int Size { get; set; }
        }

        public enum TestEnum
        {
            One,
            [Description("num-two")]
            Two
        }
    }
}