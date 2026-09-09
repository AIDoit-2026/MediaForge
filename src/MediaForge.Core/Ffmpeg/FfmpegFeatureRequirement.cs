namespace MediaForge.Core.Ffmpeg;

/// <summary>A concrete FFmpeg capability required before a conversion can be queued.</summary>
public sealed record FfmpegFeatureRequirement(string FeatureName, FfmpegFeatureKind Kind, string CapabilityName);

public enum FfmpegFeatureKind
{
    Encoder,
    Muxer,
    Filter
}
