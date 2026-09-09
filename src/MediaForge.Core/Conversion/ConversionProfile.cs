namespace MediaForge.Core.Conversion;

/// <summary>
/// UI-independent conversion choices. Validity and FFmpeg support are evaluated by the conversion validator.
/// </summary>
public sealed record ConversionProfile(
    string OutputContainer,
    VideoEncodingSettings Video,
    AudioEncodingSettings Audio,
    VideoFilterSettings Filters,
    TimeRange? TimeRange,
    string? ExternalSrtPath)
{
    public static ConversionProfile CreateDefault() => new(
        OutputContainer: "mp4",
        Video: VideoEncodingSettings.EncodeWith("libx264"),
        Audio: AudioEncodingSettings.EncodeWith("aac"),
        Filters: VideoFilterSettings.None,
        TimeRange: null,
        ExternalSrtPath: null);
}
