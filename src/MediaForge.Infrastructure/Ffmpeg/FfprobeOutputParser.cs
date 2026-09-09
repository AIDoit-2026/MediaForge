using System.Globalization;
using System.Text.Json;
using MediaForge.Core.Media;

namespace MediaForge.Infrastructure.Ffmpeg;

public static class FfprobeOutputParser
{
    public static MediaSourceInfo Parse(string path, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var format = root.TryGetProperty("format", out var formatElement) ? formatElement : default;
        var streams = root.TryGetProperty("streams", out var streamsElement)
            ? streamsElement.EnumerateArray().Select(ParseStream).ToArray()
            : [];

        return new MediaSourceInfo(
            path,
            GetString(format, "format_name"),
            GetDuration(format, "duration"),
            GetLong(format, "size"),
            GetLong(format, "bit_rate"),
            streams,
            GetTags(format));
    }

    private static MediaStreamInfo ParseStream(JsonElement stream) => new(
        GetInt(stream, "index") ?? -1,
        ParseType(GetString(stream, "codec_type")),
        GetString(stream, "codec_name") ?? "unknown",
        GetTag(stream, "language"),
        GetTag(stream, "title"),
        stream.TryGetProperty("disposition", out var disposition) && GetInt(disposition, "default") == 1,
        GetInt(stream, "width"),
        GetInt(stream, "height"),
        GetString(stream, "avg_frame_rate"),
        GetString(stream, "pix_fmt"),
        GetInt(stream, "sample_rate"),
        GetInt(stream, "channels"),
        GetString(stream, "channel_layout"),
        GetLong(stream, "bit_rate"),
        stream.TryGetProperty("disposition", out var attachmentDisposition) && GetInt(attachmentDisposition, "attached_pic") == 1);

    private static MediaStreamType ParseType(string? type) => type switch
    {
        "video" => MediaStreamType.Video,
        "audio" => MediaStreamType.Audio,
        "subtitle" => MediaStreamType.Subtitle,
        "data" => MediaStreamType.Data,
        _ => MediaStreamType.Unknown
    };

    private static string? GetTag(JsonElement element, string name) =>
        element.TryGetProperty("tags", out var tags) ? GetString(tags, name) : null;

    private static IReadOnlyDictionary<string, string> GetTags(JsonElement element) =>
        element.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Object
            ? tags.EnumerateObject()
                .Where(property => GetText(property.Value) is not null)
                .ToDictionary(property => property.Name, property => GetText(property.Value)!, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static string? GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            ? GetText(value)
            : null;

    private static int? GetInt(JsonElement element, string name) =>
        GetString(element, name) is { } value && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static long? GetLong(JsonElement element, string name) =>
        GetString(element, name) is { } value && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static TimeSpan? GetDuration(JsonElement element, string name) =>
        GetString(element, name) is { } value && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;

    private static string? GetText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        _ => null
    };
}
