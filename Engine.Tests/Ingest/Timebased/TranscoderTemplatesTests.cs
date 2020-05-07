using System;
using DLCS.Model.Assets;
using Engine.Ingest.Timebased;
using FluentAssertions;
using Xunit;

namespace Engine.Tests.Ingest.Timebased
{
    public class TranscoderTemplatesTests
    {
        [Fact]
        public void GetDestinationPath_Null_IfPresetNoInExpectedFormat()
        {
            // Act
            var (template, preset) = TranscoderTemplates.ProcessPreset("video/mpg", new Asset(), "mp3preset");
            
            // Assert
            template.Should().BeNull();
            preset.Should().BeNull();
        }
        
        [Fact]
        public void GetDestinationPath_ReturnsExpected_IfAudio()
        {
            // Arrange
            var asset = new Asset {Customer = 1, Space = 5, Id = "1/5/foo"};
            const string expected = "1/5/foo/full/max/default.mp3";
            
            // Act
            var (template, preset) =
                TranscoderTemplates.ProcessPreset("audio/wav", asset, "my-preset(mp3)");
            
            // Assert
            template.Should().Be(expected);
            preset.Should().Be("my-preset");
        }
        
        [Fact]
        public void GetDestinationPath_ReturnsExpected_IfVideo()
        {
            // Arrange
            var asset = new Asset {Customer = 1, Space = 5, Id = "1/5/foo"};
            const string expected = "1/5/foo/full/full/max/max/0/default.webm";
            
            // Act
            var (template, preset) =
                TranscoderTemplates.ProcessPreset("video/mpeg", asset, "my-preset(webm)");
            
            // Assert
            template.Should().Be(expected);
            preset.Should().Be("my-preset");
        }
        
        [Fact]
        public void GetDestinationPath_Throws_IfNonAudioOrVideo()
        {
            // Act
            Action action = () =>
                TranscoderTemplates.ProcessPreset("binary/octet-stream", new Asset(), "my-preset(webm)");

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Unable to determine target location for mediaType 'binary/octet-stream'");
        }
    }
}