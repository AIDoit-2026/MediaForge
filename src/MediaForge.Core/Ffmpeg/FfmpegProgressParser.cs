using System.Globalization;

namespace MediaForge.Core.Ffmpeg;

public sealed class FfmpegProgressParser
{
    public IEnumerable<FfmpegProgressUpdate> Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex];
            var value = line[(separatorIndex + 1)..];
            values[key] = value;

            if (key == "progress")
            {
                yield return CreateUpdate(values);
                values = new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }
    }

    private static FfmpegProgressUpdate CreateUpdate(IReadOnlyDictionary<string, string> values)
    {
        var outputTime = ParseOutputTime(values);
        return new FfmpegProgressUpdate(
            Frame: ParseLong(values, "frame"),
            FramesPerSecond: ParseDouble(values, "fps"),
            TotalSizeBytes: ParseLong(values, "total_size"),
            OutputTime: outputTime,
            Bitrate: ParseText(values, "bitrate"),
            Speed: ParseSpeed(values),
            IsCompleted: values.TryGetValue("progress", out var progress) && progress == "end",
            Values: new Dictionary<string, string>(values, StringComparer.Ordinal));
    }

    private static TimeSpan? ParseOutputTime(IReadOnlyDictionary<string, string> values)
    {
        if (values.TryGetValue("out_time", out var outputTime) &&
            TimeSpan.TryParse(outputTime, CultureInfo.InvariantCulture, out var parsedOutputTime))
        {
            return parsedOutputTime;
        }

        // FFmpeg currently emits microseconds for both keys, despite the legacy name out_time_ms.
        var microseconds = ParseLong(values, "out_time_us") ?? ParseLong(values, "out_time_ms");
        return microseconds is null ? null : TimeSpan.FromMicroseconds(microseconds.Value);
    }

    private static long? ParseLong(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) &&
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static double? ParseDouble(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) &&
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static string? ParseText(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && value != "N/A" ? value : null;

    private static double? ParseSpeed(IReadOnlyDictionary<string, string> values)
    {
        if (!values.TryGetValue("speed", out var speed))
        {
            return null;
        }

        return double.TryParse(
            speed.Trim().TrimEnd('x'),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }
}
