using DLCS.Model.Assets;
using Engine.Ingest;
using FluentAssertions;
using Xunit;

namespace Engine.Tests.Ingest
{
    public class TemplatedFoldersTests
    {
        [Fact]
        public void GenerateTemplate_ReturnsExpected_ImageNameUnaltered_IfImageName8CharsOrLess()
        {
            // Arrange
            char s = System.IO.Path.DirectorySeparatorChar;
            var root = "folder";
            var asset = new Asset {Customer = 10, Space = 20, Id = "foobarba"};
            var template = $"{s}{{root}}{s}{{customer}}{s}{{space}}{s}{{image}}";
            var expected = $"{s}folder{s}10{s}20{s}foobarba";
            
            // Act
            var result = TemplatedFolders.GenerateTemplate(template, root, asset);
            
            // Assert
            result.Should().Be(expected);
        }
        
        [Fact]
        public void GenerateTemplate_ReturnsExpected_ImageNameAltered_IfImageName8CharsOrLess()
        {
            // Arrange
            char s = System.IO.Path.DirectorySeparatorChar;
            var root = "folder";
            var asset = new Asset {Customer = 10, Space = 20, Id = "foobarbazqux"};
            var template = $"{s}{{root}}{s}{{customer}}{s}{{space}}{s}{{image}}";
            var expected = $"{s}folder{s}10{s}20{s}fo{s}ob{s}ar{s}ba{s}foobarbazqux";
            
            // Act
            var result = TemplatedFolders.GenerateTemplate(template, root, asset);
            
            // Assert
            result.Should().Be(expected);
        }
        
        [Fact]
        public void GenerateTemplate_ReturnsExpected_ImageNameNotReplaced_IfReplaceImageNameFalse()
        {
            // Arrange
            char s = System.IO.Path.DirectorySeparatorChar;
            var root = "folder";
            var asset = new Asset {Customer = 10, Space = 20, Id = "foobarbazqux"};
            var template = $"{s}{{root}}{s}{{customer}}{s}{{space}}{s}{{image}}";
            var expected = $"{s}folder{s}10{s}20{s}{{image}}";
            
            // Act
            var result = TemplatedFolders.GenerateTemplate(template, root, asset, false);
            
            // Assert
            result.Should().Be(expected);
        }
    }
}