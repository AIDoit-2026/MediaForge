namespace MediaForge.Core.Ffmpeg;

public interface IFfmpegToolResolver
{
    FfmpegToolResolution Resolve(string? configuredDirectory);
}
