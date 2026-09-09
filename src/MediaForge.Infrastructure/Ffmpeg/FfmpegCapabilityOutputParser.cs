using System.Text.RegularExpressions;

namespace MediaForge.Infrastructure.Ffmpeg;

public static partial class FfmpegCapabilityOutputParser
{
    public static IReadOnlyList<string> ParseCodecNames(string output) =>
        ParseNames(output, CodecLinePattern());

    public static IReadOnlyList<string> ParseFormatNames(string output) =>
        ParseNames(output, FormatLinePattern());

    public static IReadOnlyList<string> ParseFilterNames(string output) =>
        ParseNames(output, FilterLinePattern());

    public static IReadOnlyList<string> ParseHardwareAccelerationNames(string output) =>
        output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SkipWhile(line => !line.EndsWith(':'))
            .Skip(1)
            .Where(line => !line.Contains(' '))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<string> ParseNames(string output, Regex pattern) =>
        output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.Contains('='))
            .Select(line => pattern.Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    [GeneratedRegex(@"^\s*[VAS][A-Z.]{5}\s+(?<name>\S+)", RegexOptions.CultureInvariant)]
    private static partial Regex CodecLinePattern();

    [GeneratedRegex(@"^\s*[DE.]{1,3}\s+(?<name>\S+)", RegexOptions.CultureInvariant)]
    private static partial Regex FormatLinePattern();

    [GeneratedRegex(@"^\s*[TSC.]{3}\s+(?<name>\S+)", RegexOptions.CultureInvariant)]
    private static partial Regex FilterLinePattern();
}
