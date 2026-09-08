namespace MediaForge.Core.Ffmpeg;

public interface IFfmpegVersionReader
{
    Task<FfmpegVersionReadResult> ReadAsync(
        FfmpegToolset toolset,
        CancellationToken cancellationToken = default);
}
