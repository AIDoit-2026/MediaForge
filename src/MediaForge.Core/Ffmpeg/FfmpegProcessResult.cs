namespace MediaForge.Core.Ffmpeg;

public sealed record FfmpegProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
