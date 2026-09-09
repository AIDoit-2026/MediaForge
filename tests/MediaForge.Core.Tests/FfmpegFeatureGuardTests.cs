using MediaForge.Core.Ffmpeg;

namespace MediaForge.Core.Tests;

public sealed class FfmpegFeatureGuardTests
{
    [Theory]
    [InlineData(FfmpegFeatureKind.Encoder, "libx264")]
    [InlineData(FfmpegFeatureKind.Muxer, "mp4")]
    [InlineData(FfmpegFeatureKind.Filter, "subtitles")]
    public void Check_returns_supported_when_capability_is_present(FfmpegFeatureKind kind, string capability)
    {
        var capabilities = new FfmpegCapabilities(["libx264"], [], ["mp4"], [], ["subtitles"], []);

        var result = new FfmpegFeatureGuard().Check(capabilities, new("Selected feature", kind, capability));

        Assert.True(result.IsSupported);
        Assert.Null(result.UserAction);
    }

    [Fact]
    public void Check_blocks_missing_capability_with_update_action()
    {
        var result = new FfmpegFeatureGuard().Check(
            FfmpegCapabilities.Empty,
            new("H.264 encoding", FfmpegFeatureKind.Encoder, "libx264"));

        Assert.False(result.IsSupported);
        Assert.Equal("libx264", result.MissingCapability);
        Assert.Contains("Update or replace FFmpeg", result.UserAction);
    }
}
