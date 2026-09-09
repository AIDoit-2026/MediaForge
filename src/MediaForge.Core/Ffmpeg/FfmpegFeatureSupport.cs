namespace MediaForge.Core.Ffmpeg;

public sealed record FfmpegFeatureSupport(bool IsSupported, string? MissingCapability, string? UserAction)
{
    public static FfmpegFeatureSupport Supported { get; } = new(true, null, null);
}
