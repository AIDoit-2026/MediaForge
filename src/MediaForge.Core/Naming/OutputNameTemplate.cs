using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaForge.Core.Naming;

public static partial class OutputNameTemplate
{
    private static readonly SearchValues<char> InvalidFileNameChars =
        SearchValues.Create(Path.GetInvalidFileNameChars());

    public static string Render(string template, OutputNameContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentNullException.ThrowIfNull(context);

        var rendered = TokenPattern().Replace(template, match =>
        {
            var value = match.Groups[1].Value switch
            {
                "name" => context.Name,
                "ext" => context.Extension.TrimStart('.'),
                "preset" => context.Preset,
                "index" => context.Index.ToString(CultureInfo.InvariantCulture),
                "date" => context.CreatedAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                "width" => context.Width.ToString(CultureInfo.InvariantCulture),
                "height" => context.Height.ToString(CultureInfo.InvariantCulture),
                "resolution" => FormattableString.Invariant($"{context.Width}x{context.Height}"),
                "video_codec" => context.VideoCodec,
                "audio_codec" => context.AudioCodec,
                var unknown => throw new FormatException($"Unknown output name token '{unknown}'.")
            };

            return Sanitize(value);
        });

        if (rendered.IndexOfAny(['{', '}']) >= 0)
        {
            throw new FormatException("Output name template contains an invalid token.");
        }

        return rendered;
    }

    private static string Sanitize(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            result.Append(InvalidFileNameChars.Contains(character) ? '_' : character);
        }

        return result.ToString();
    }

    [GeneratedRegex("\\{([a-z_]+)\\}", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
