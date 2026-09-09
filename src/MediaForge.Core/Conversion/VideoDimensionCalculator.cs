namespace MediaForge.Core.Conversion;

/// <summary>
/// Computes filter output geometry in FFmpeg filter order: crop, scale, rotation, then padding.
/// </summary>
public static class VideoDimensionCalculator
{
    public static VideoDimensionCalculationResult Calculate(FrameSize sourceSize, VideoFilterSettings filters)
    {
        ArgumentNullException.ThrowIfNull(sourceSize);
        ArgumentNullException.ThrowIfNull(filters);

        var issues = new List<VideoDimensionIssue>();
        if (!IsPositive(sourceSize))
        {
            issues.Add(new VideoDimensionIssue("Video.InvalidSourceDimensions", "source.video"));
            return new VideoDimensionCalculationResult(null, issues);
        }

        var size = sourceSize;
        if (filters.Crop is { } crop)
        {
            if (crop.Width <= 0 || crop.Height <= 0 || crop.X < 0 || crop.Y < 0 ||
                crop.X + crop.Width > size.Width || crop.Y + crop.Height > size.Height)
            {
                issues.Add(new VideoDimensionIssue("Video.InvalidCrop", "filters.crop"));
            }
            else
            {
                size = new FrameSize(crop.Width, crop.Height);
            }
        }

        if (filters.Scale is { } scale)
        {
            if (!IsPositive(scale))
            {
                issues.Add(new VideoDimensionIssue("Video.InvalidDimensions", "filters.scale"));
            }
            else
            {
                size = scale;
            }
        }

        if (filters.Rotation is VideoRotation.Clockwise90 or VideoRotation.CounterClockwise90)
        {
            size = new FrameSize(size.Height, size.Width);
        }

        if (filters.Padding is { } padding)
        {
            if (!IsPositive(padding.Canvas) || padding.X < 0 || padding.Y < 0 ||
                padding.X + size.Width > padding.Canvas.Width || padding.Y + size.Height > padding.Canvas.Height)
            {
                issues.Add(new VideoDimensionIssue("Video.InvalidPadding", "filters.padding"));
            }
            else
            {
                size = padding.Canvas;
            }
        }

        return new VideoDimensionCalculationResult(issues.Count == 0 ? size : null, issues);
    }

    private static bool IsPositive(FrameSize size) => size.Width > 0 && size.Height > 0;
}
