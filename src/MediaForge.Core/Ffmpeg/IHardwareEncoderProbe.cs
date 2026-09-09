namespace MediaForge.Core.Ffmpeg;

public interface IHardwareEncoderProbe
{
    Task<IReadOnlyList<HardwareEncoderAvailability>> ProbeAsync(
        FfmpegToolset toolset,
        FfmpegCapabilities capabilities,
        CancellationToken cancellationToken = default);
}
