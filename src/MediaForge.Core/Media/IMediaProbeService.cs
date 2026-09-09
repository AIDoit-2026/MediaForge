namespace MediaForge.Core.Media;

public interface IMediaProbeService
{
    Task<MediaSourceInfo> ProbeAsync(string path, CancellationToken cancellationToken = default);
}
