namespace MediaForge.Core.Ffmpeg;

public interface IFfmpegCapabilityService
{
    Task<FfmpegCapabilities> GetAsync(
        FfmpegToolset toolset,
        bool forceRefresh,
        CancellationToken cancellationToken = default);
}
