namespace MediaForge.Core.Ffmpeg;

public interface IFfmpegProcessRunner
{
    Task<FfmpegProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}
