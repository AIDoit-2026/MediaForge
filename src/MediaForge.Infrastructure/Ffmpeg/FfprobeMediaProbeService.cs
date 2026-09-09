using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Media;

namespace MediaForge.Infrastructure.Ffmpeg;

public sealed class FfprobeMediaProbeService : IMediaProbeService
{
    private readonly FfmpegToolset _toolset;
    private readonly IFfmpegProcessRunner _processRunner;

    public FfprobeMediaProbeService(FfmpegToolset toolset, IFfmpegProcessRunner processRunner)
    {
        ArgumentNullException.ThrowIfNull(toolset);
        ArgumentNullException.ThrowIfNull(processRunner);
        _toolset = toolset;
        _processRunner = processRunner;
    }

    public async Task<MediaSourceInfo> ProbeAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var result = await _processRunner.RunAsync(
            _toolset.FfprobePath,
            ["-v", "error", "-show_format", "-show_streams", "-of", "json", path],
            cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"FFprobe failed: {result.StandardError}");
        }

        return FfprobeOutputParser.Parse(path, result.StandardOutput);
    }
}
