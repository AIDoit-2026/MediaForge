namespace MediaForge.Core.Media;

public sealed record MediaSourceInfo(
    string Path,
    string? Container,
    TimeSpan? Duration,
    long? Size,
    long? BitRate,
    IReadOnlyList<MediaStreamInfo> Streams);
