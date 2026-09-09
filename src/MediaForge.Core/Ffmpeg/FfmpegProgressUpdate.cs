namespace MediaForge.Core.Ffmpeg;

public sealed record FfmpegProgressUpdate(
    long? Frame,
    double? FramesPerSecond,
    long? TotalSizeBytes,
    TimeSpan? OutputTime,
    string? Bitrate,
    double? Speed,
    bool IsCompleted,
    IReadOnlyDictionary<string, string> Values);
