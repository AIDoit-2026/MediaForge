using System.Globalization;
using MediaForge.Core.Configuration;

namespace MediaForge.Core.Conversion;

/// <summary>Central mapping from a persisted parameter snapshot to an executable conversion profile.</summary>
public static class ConversionProfileFactory
{
    public static ConversionProfile Create(ConversionParameterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var container = snapshot.OutputContainer;
        var isAudioOnly = IsAudioOnlyContainer(container);
        var values = snapshot.Values;

        var video = isAudioOnly || TryGetValue(values, "videoMode") == "none"
            ? VideoEncodingSettings.Exclude
            : CreateVideoSettings(values);

        var audio = CreateAudioSettings(values, isAudioOnly);
        return new ConversionProfile(container, video, audio, VideoFilterSettings.None, null, null);
    }

    private static VideoEncodingSettings CreateVideoSettings(IReadOnlyDictionary<string, string> values)
    {
        var encoder = TryGetValue(values, "videoEncoder");
        if (TryGetValue(values, "videoMode") == "copy" || string.IsNullOrWhiteSpace(encoder))
        {
            return VideoEncodingSettings.Copy;
        }

        var qualityMode = TryGetValue(values, "qualityMode") == "bitrate"
            ? VideoQualityMode.TargetBitrate
            : VideoQualityMode.ConstantQuality;
        var constantQuality = TryGetValue(values, "crf") is { } crf && int.TryParse(crf, NumberStyles.Integer, CultureInfo.InvariantCulture, out var crfValue)
            ? crfValue
            : (int?)null;
        var bitrate = TryGetValue(values, "videoBitrateKbps") is { } bitrateKbps &&
            int.TryParse(bitrateKbps, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bitrateValue)
            ? bitrateValue
            : (int?)null;
        return new VideoEncodingSettings(
            StreamProcessingMode.Encode,
            encoder,
            qualityMode,
            ConstantQuality: qualityMode == VideoQualityMode.ConstantQuality ? constantQuality ?? 23 : null,
            BitrateKbps: qualityMode == VideoQualityMode.TargetBitrate ? bitrate : null,
            Preset: null,
            TwoPass: false);
    }

    private static AudioEncodingSettings CreateAudioSettings(IReadOnlyDictionary<string, string> values, bool isAudioOnly)
    {
        // Audio-only containers have no video stream to encode, so the audio stream carries
        // the conversion; treat a missing audioEncoder as a copy rather than silence.
        var encoder = TryGetValue(values, "audioEncoder");
        if (isAudioOnly && string.IsNullOrWhiteSpace(encoder))
        {
            return AudioEncodingSettings.Copy;
        }

        if (TryGetValue(values, "audioMode") == "copy" || string.IsNullOrWhiteSpace(encoder))
        {
            return AudioEncodingSettings.Copy;
        }

        var bitrate = TryGetValue(values, "audioBitrateKbps") is { } bitrateKbps &&
            int.TryParse(bitrateKbps, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bitrateValue)
            ? bitrateValue
            : (int?)null;
        return new AudioEncodingSettings(StreamProcessingMode.Encode, encoder, null, null, bitrate);
    }

    private static string? TryGetValue(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : null;

    private static bool IsAudioOnlyContainer(string container) => container is "mp3" or "m4a" or "flac" or "wav" or "opus";
}
