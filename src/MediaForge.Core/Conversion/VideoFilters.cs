namespace MediaForge.Core.Conversion;

public sealed record FrameSize(int Width, int Height);

public sealed record CropRectangle(int Width, int Height, int X, int Y);

public sealed record PaddingSettings(FrameSize Canvas, int X, int Y, string Color);

public enum VideoRotation
{
    None,
    Clockwise90,
    CounterClockwise90,
    Rotate180
}

public sealed record VideoFilterSettings(
    CropRectangle? Crop,
    FrameSize? Scale,
    VideoRotation Rotation,
    PaddingSettings? Padding)
{
    public static VideoFilterSettings None { get; } = new(null, null, VideoRotation.None, null);
}
