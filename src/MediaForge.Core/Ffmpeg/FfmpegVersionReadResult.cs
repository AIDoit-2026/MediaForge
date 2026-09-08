namespace MediaForge.Core.Ffmpeg;

public sealed record FfmpegVersionReadResult(Version? Version, string? ErrorCode, string? Details)
{
    public const string UnrecognizedVersion = nameof(UnrecognizedVersion);
    public const string VersionCommandFailed = nameof(VersionCommandFailed);
    public const string VersionCommandTimedOut = nameof(VersionCommandTimedOut);

    public bool IsSuccess => Version is not null;

    public static FfmpegVersionReadResult Success(Version version) => new(version, null, null);

    public static FfmpegVersionReadResult Failure(string errorCode, string? details = null) =>
        new(null, errorCode, details);
}
