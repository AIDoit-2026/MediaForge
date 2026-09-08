using MediaForge.Core.Ffmpeg;

namespace MediaForge.Infrastructure.Ffmpeg;

public sealed class FfmpegToolResolver : IFfmpegToolResolver
{
    public const string ToolsNotFound = nameof(ToolsNotFound);

    private readonly string _applicationDirectory;

    public FfmpegToolResolver(string applicationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);
        _applicationDirectory = Path.GetFullPath(applicationDirectory);
    }

    public FfmpegToolResolution Resolve(string? configuredDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            var configuredToolset = TryCreateToolset(
                configuredDirectory,
                FfmpegToolSource.ConfiguredDirectory);

            if (configuredToolset is not null)
            {
                return FfmpegToolResolution.Success(configuredToolset);
            }
        }

        var applicationToolset = TryCreateToolset(
            _applicationDirectory,
            FfmpegToolSource.ApplicationDirectory);

        if (applicationToolset is not null)
        {
            return FfmpegToolResolution.Success(applicationToolset);
        }

        return FfmpegToolResolution.Failure(ToolsNotFound);
    }

    private static FfmpegToolset? TryCreateToolset(string directory, FfmpegToolSource source)
    {
        string normalizedDirectory;
        try
        {
            normalizedDirectory = Path.GetFullPath(directory);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        var ffmpegPath = Path.Combine(normalizedDirectory, "ffmpeg.exe");
        var ffprobePath = Path.Combine(normalizedDirectory, "ffprobe.exe");

        return File.Exists(ffmpegPath) && File.Exists(ffprobePath)
            ? new FfmpegToolset(normalizedDirectory, ffmpegPath, ffprobePath, source)
            : null;
    }
}
