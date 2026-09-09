using MediaForge.Core.Conversion;

namespace MediaForge.Core.Tests;

public sealed class VideoDimensionCalculatorTests
{
    [Fact]
    public void Calculator_applies_crop_scale_rotation_and_padding_in_deterministic_order()
    {
        var filters = new VideoFilterSettings(
            new CropRectangle(1600, 900, 100, 50),
            new FrameSize(1280, 720),
            VideoRotation.Clockwise90,
            new PaddingSettings(new FrameSize(1080, 1920), 180, 320, "black"));

        var result = VideoDimensionCalculator.Calculate(new FrameSize(1920, 1080), filters);

        Assert.True(result.IsValid);
        Assert.Equal(new FrameSize(1080, 1920), result.OutputSize);
    }

    [Fact]
    public void Calculator_rejects_crop_outside_source_bounds()
    {
        var result = VideoDimensionCalculator.Calculate(
            new FrameSize(1920, 1080),
            new VideoFilterSettings(new CropRectangle(1000, 800, 1000, 500), null, VideoRotation.None, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "Video.InvalidCrop" && issue.Field == "filters.crop");
    }

    [Fact]
    public void Calculator_rejects_padding_that_cannot_contain_the_filtered_frame()
    {
        var result = VideoDimensionCalculator.Calculate(
            new FrameSize(1280, 720),
            new VideoFilterSettings(null, null, VideoRotation.None, new PaddingSettings(new FrameSize(1280, 720), 1, 0, "black")));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "Video.InvalidPadding");
    }
}
