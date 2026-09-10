namespace MediaForge.Core.Ffmpeg;

/// <summary>Optional line-streaming capability for long-running FFmpeg processes.</summary>
public interface IStreamingFfmpegProcessRunner : IFfmpegProcessRunner
{
    Task<FfmpegProcessResult> RunWithOutputObserverAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        Action<string>? standardOutputLine,
        CancellationToken cancellationToken = default);
}
