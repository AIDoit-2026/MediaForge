using MediaForge.Core.Naming;

namespace MediaForge.Core.Tests;

public sealed class OutputNameTemplateTests
{
    [Fact]
    public void Render_expands_all_supported_tokens_with_stable_values()
    {
        var context = new OutputNameContext(
            Name: "holiday",
            Extension: "mp4",
            Preset: "Web/High",
            Index: 7,
            CreatedAt: new DateTimeOffset(2026, 9, 8, 14, 30, 0, TimeSpan.FromHours(8)),
            Width: 1920,
            Height: 1080,
            VideoCodec: "libx264",
            AudioCodec: "aac");

        var result = OutputNameTemplate.Render(
            "{date}_{index}_{name}_{preset}_{resolution}_{width}x{height}_{video_codec}_{audio_codec}.{ext}",
            context);

        Assert.Equal("20260908_7_holiday_Web_High_1920x1080_1920x1080_libx264_aac.mp4", result);
    }

    [Fact]
    public void Render_rejects_an_unknown_token()
    {
        var context = new OutputNameContext("clip", "mp4", "Default", 1,
            DateTimeOffset.UnixEpoch, 1280, 720, "h264_nvenc", "aac");

        var error = Assert.Throws<FormatException>(
            () => OutputNameTemplate.Render("{name}_{unknown}.{ext}", context));

        Assert.Contains("unknown", error.Message, StringComparison.Ordinal);
    }
}
