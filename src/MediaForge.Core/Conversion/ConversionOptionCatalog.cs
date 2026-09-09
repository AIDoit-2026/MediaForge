using MediaForge.Core.Ffmpeg;

namespace MediaForge.Core.Conversion;

/// <summary>
/// Maps the FFmpeg capability report to conservative MVP conversion choices.
/// The catalog intentionally never manufactures an encoder or muxer that the current toolset did not report.
/// </summary>
public static class ConversionOptionCatalog
{
    private static readonly ConversionFormatOption[] KnownFormats =
    [
        new("mp4", "MP4", "mp4", ["mp4"], ["libx264", "libx265", "h264_nvenc", "hevc_nvenc", "h264_qsv", "h264_amf"], ["aac"]),
        new("mkv", "Matroska", "mkv", ["matroska"], ["libx264", "libx265", "libsvtav1", "libaom-av1", "libvpx-vp9", "h264_nvenc", "hevc_nvenc", "h264_qsv", "h264_amf"], ["aac", "libmp3lame", "flac", "libopus"]),
        new("mov", "MOV", "mov", ["mov"], ["libx264", "libx265", "h264_nvenc", "hevc_nvenc", "h264_qsv", "h264_amf"], ["aac", "alac", "pcm_s16le"]),
        new("webm", "WebM", "webm", ["webm"], ["libvpx-vp9", "libsvtav1", "libaom-av1"], ["libopus"]),
        new("mp3", "MP3", "mp3", ["mp3"], [], ["libmp3lame"]),
        new("m4a", "M4A / AAC", "m4a", ["ipod", "mp4"], [], ["aac"]),
        new("flac", "FLAC", "flac", ["flac"], [], ["flac"]),
        new("wav", "WAV", "wav", ["wav"], [], ["pcm_s16le"]),
        new("opus", "Opus", "opus", ["opus"], [], ["libopus"])
    ];

    public static ConversionOptions Create(
        FfmpegCapabilities capabilities,
        IEnumerable<HardwareEncoderAvailability>? hardwareAvailability = null)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        var encoders = capabilities.Encoders.ToHashSet(StringComparer.Ordinal);
        var muxers = capabilities.Muxers.ToHashSet(StringComparer.Ordinal);
        var usableHardware = (hardwareAvailability ?? [])
            .Where(availability => availability.IsAvailable)
            .Select(availability => availability.EncoderName)
            .ToHashSet(StringComparer.Ordinal);

        var formats = KnownFormats
            .Where(format => format.RequiredMuxers.Any(muxers.Contains))
            .Select(format => format with
            {
                VideoEncoders = FilterEncoders(format.VideoEncoders, encoders, usableHardware),
                AudioEncoders = FilterEncoders(format.AudioEncoders, encoders, usableHardware)
            })
            .Where(format => format.VideoEncoders.Count > 0 || format.AudioEncoders.Count > 0)
            .ToArray();

        return new ConversionOptions(
            formats,
            formats.SelectMany(format => format.VideoEncoders).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            formats.SelectMany(format => format.AudioEncoders).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    }

    private static IReadOnlyList<string> FilterEncoders(
        IEnumerable<string> candidates,
        ISet<string> declaredEncoders,
        ISet<string> usableHardware) => candidates
        .Where(encoder => declaredEncoders.Contains(encoder))
        .Where(encoder => !IsHardwareEncoder(encoder) || usableHardware.Contains(encoder))
        .ToArray();

    private static bool IsHardwareEncoder(string encoder) =>
        encoder.EndsWith("_nvenc", StringComparison.Ordinal) ||
        encoder.EndsWith("_qsv", StringComparison.Ordinal) ||
        encoder.EndsWith("_amf", StringComparison.Ordinal);
}
