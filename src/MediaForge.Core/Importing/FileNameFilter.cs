using System.Text.RegularExpressions;

namespace MediaForge.Core.Importing;

public sealed class FileNameFilter
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);
    private readonly FileNameFilterMode _mode;
    private readonly string _expression;
    private readonly Regex? _pattern;

    private FileNameFilter(FileNameFilterMode mode, string expression, Regex? pattern)
    {
        _mode = mode;
        _expression = expression;
        _pattern = pattern;
    }

    public static FileNameFilter Create(FileNameFilterMode mode, string? expression)
    {
        expression ??= string.Empty;
        if (mode != FileNameFilterMode.All && string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("A file name filter expression is required.", nameof(expression));
        }

        try
        {
            var pattern = mode switch
            {
                FileNameFilterMode.Wildcard => new Regex(
                    $"^{Regex.Escape(expression).Replace("\\*", ".*").Replace("\\?", ".")}$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    MatchTimeout),
                FileNameFilterMode.RegularExpression => new Regex(
                    expression,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    MatchTimeout),
                _ => null
            };

            return new FileNameFilter(mode, expression, pattern);
        }
        catch (ArgumentException error) when (mode == FileNameFilterMode.RegularExpression)
        {
            throw new ArgumentException("The regular expression is invalid.", nameof(expression), error);
        }
    }

    public bool IsMatch(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fileName = Path.GetFileName(path);

        return _mode switch
        {
            FileNameFilterMode.All => true,
            FileNameFilterMode.Keyword => fileName.Contains(_expression, StringComparison.OrdinalIgnoreCase),
            FileNameFilterMode.Wildcard or FileNameFilterMode.RegularExpression => _pattern!.IsMatch(fileName),
            _ => throw new InvalidOperationException($"Unsupported file name filter mode '{_mode}'.")
        };
    }
}
