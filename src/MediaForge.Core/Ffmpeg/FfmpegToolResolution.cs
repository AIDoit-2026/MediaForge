namespace MediaForge.Core.Ffmpeg;

public sealed record FfmpegToolResolution(FfmpegToolset? Toolset, string? ErrorCode)
{
    public bool IsSuccess => Toolset is not null;

    public static FfmpegToolResolution Success(FfmpegToolset toolset) =>
        new(toolset, ErrorCode: null);

    public static FfmpegToolResolution Failure(string errorCode) =>
        new(Toolset: null, errorCode);
}
