using MediaForge.Core.Ffmpeg;

namespace MediaForge.Core.Tests;

public sealed class FfmpegProgressParserTests
{
    [Fact]
    public void Parse_emits_an_update_for_each_progress_terminator()
    {
        var parser = new FfmpegProgressParser();

        var updates = parser.Parse(
        [
            "frame=30",
            "fps=29.97",
            "total_size=4096",
            "out_time_us=1000000",
            "bitrate=32.8kbits/s",
            "speed= 1.25x",
            "progress=continue",
            "frame=60",
            "out_time=00:00:02.000000",
            "speed=N/A",
            "progress=end"
        ]).ToArray();

        Assert.Collection(
            updates,
            first =>
            {
                Assert.Equal(30, first.Frame);
                Assert.Equal(29.97, first.FramesPerSecond);
                Assert.Equal(4096, first.TotalSizeBytes);
                Assert.Equal(TimeSpan.FromSeconds(1), first.OutputTime);
                Assert.Equal("32.8kbits/s", first.Bitrate);
                Assert.Equal(1.25, first.Speed);
                Assert.False(first.IsCompleted);
            },
            second =>
            {
                Assert.Equal(60, second.Frame);
                Assert.Equal(TimeSpan.FromSeconds(2), second.OutputTime);
                Assert.Null(second.Speed);
                Assert.True(second.IsCompleted);
            });
    }

    [Fact]
    public void Parse_ignores_malformed_lines_and_uses_the_legacy_microsecond_key()
    {
        var parser = new FfmpegProgressParser();

        var update = Assert.Single(parser.Parse(
        [
            "invalid line",
            "out_time_ms=500000",
            "progress=end"
        ]));

        Assert.Equal(TimeSpan.FromMilliseconds(500), update.OutputTime);
        Assert.True(update.IsCompleted);
    }
}
