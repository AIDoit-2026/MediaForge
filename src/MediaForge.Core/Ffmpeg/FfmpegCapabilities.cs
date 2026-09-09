namespace MediaForge.Core.Ffmpeg;

public sealed record FfmpegCapabilities(
    IReadOnlyList<string> Encoders,
    IReadOnlyList<string> Decoders,
    IReadOnlyList<string> Muxers,
    IReadOnlyList<string> Demuxers,
    IReadOnlyList<string> Filters,
    IReadOnlyList<string> HardwareAccelerations)
{
    public static FfmpegCapabilities Empty { get; } = new([], [], [], [], [], []);
}
