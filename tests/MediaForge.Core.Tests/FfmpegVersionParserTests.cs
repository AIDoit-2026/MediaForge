using MediaForge.Core.Ffmpeg;

namespace MediaForge.Core.Tests;

public sealed class FfmpegVersionParserTests
{
    [Theory]
    [InlineData("ffmpeg version 7.1.1 Copyright (c) 2000-2025 the FFmpeg developers", "7.1.1")]
    [InlineData("ffmpeg version 6.0-full_build-www.gyan.dev", "6.0")]
    public void Parse_extracts_a_numeric_version_from_the_banner(string banner, string expected)
    {
        var result = FfmpegVersionParser.Parse(banner);

        Assert.True(result.IsSuccess);
        Assert.Equal(Version.Parse(expected), result.Version);
    }

    [Fact]
    public void Parse_returns_a_structured_failure_for_an_unrecognized_banner()
    {
        var result = FfmpegVersionParser.Parse("not an ffmpeg executable");

        Assert.False(result.IsSuccess);
        Assert.Equal(FfmpegVersionReadResult.UnrecognizedVersion, result.ErrorCode);
    }
}
