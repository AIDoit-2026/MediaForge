using System.Text.RegularExpressions;

namespace MediaForge.Core.Ffmpeg;

public static partial class FfmpegVersionParser
{
    public static FfmpegVersionReadResult Parse(string banner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(banner);

        var match = VersionPattern().Match(banner);
        return match.Success && Version.TryParse(match.Groups["version"].Value, out var version)
            ? FfmpegVersionReadResult.Success(version)
            : FfmpegVersionReadResult.Failure(FfmpegVersionReadResult.UnrecognizedVersion, banner);
    }

    [GeneratedRegex(@"^ffmpeg version (?<version>\d+(?:\.\d+){1,3})", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();
}
