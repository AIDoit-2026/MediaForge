using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Media;

namespace MediaForge.Core.Conversion;

public sealed class ConversionValidator : IConversionValidator
{
    public ConversionValidationResult Validate(
        MediaSourceInfo source,
        ConversionProfile profile,
        FfmpegCapabilities capabilities,
        IEnumerable<HardwareEncoderAvailability>? hardwareAvailability = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(capabilities);

        var issues = new List<ConversionValidationIssue>();
        var options = ConversionOptionCatalog.Create(capabilities, hardwareAvailability);
        var format = options.Formats.SingleOrDefault(option =>
            string.Equals(option.Id, profile.OutputContainer, StringComparison.OrdinalIgnoreCase));
        if (format is null)
        {
            AddError(issues, "Tool.UnsupportedContainer", "outputContainer", "Validation.Tool.UnsupportedContainer", profile.OutputContainer);
        }

        var hasVideo = source.Streams.Any(stream => stream.Type == MediaStreamType.Video);
        var hasAudio = source.Streams.Any(stream => stream.Type == MediaStreamType.Audio);
        ValidateVideo(profile, capabilities, format, hasVideo, hardwareAvailability, issues);
        ValidateAudio(profile, capabilities, format, hasAudio, issues);
        ValidateFilters(source, profile, issues);
        ValidateTimeRange(profile.TimeRange, issues);
        ValidateSubtitle(profile, issues);

        if (profile.Video.Mode == StreamProcessingMode.Exclude && profile.Audio.Mode == StreamProcessingMode.Exclude)
        {
            AddError(issues, "Profile.NoOutputStreams", "profile", "Validation.Profile.NoOutputStreams");
        }

        if (source.Duration is null)
        {
            issues.Add(new ConversionValidationIssue(
                "Source.DurationUnknown", "source.duration", ValidationSeverity.Warning, "Validation.Source.DurationUnknown"));
        }

        return new ConversionValidationResult(issues);
    }

    private static void ValidateVideo(
        ConversionProfile profile,
        FfmpegCapabilities capabilities,
        ConversionFormatOption? format,
        bool hasVideo,
        IEnumerable<HardwareEncoderAvailability>? hardwareAvailability,
        List<ConversionValidationIssue> issues)
    {
        var video = profile.Video;
        if (video.Mode != StreamProcessingMode.Exclude && !hasVideo)
        {
            AddError(issues, "Source.VideoMissing", "video.mode", "Validation.Source.VideoMissing");
        }

        if (video.Mode == StreamProcessingMode.Copy)
        {
            if (video.Encoder is not null || video.QualityMode is not null || video.ConstantQuality is not null || video.BitrateKbps is not null || video.Preset is not null || video.TwoPass)
            {
                AddError(issues, "Video.CopyHasEncodingOptions", "video", "Validation.Video.CopyHasEncodingOptions");
            }
            return;
        }

        if (video.Mode != StreamProcessingMode.Encode)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(video.Encoder) || !capabilities.Encoders.Contains(video.Encoder, StringComparer.Ordinal))
        {
            AddError(issues, "Tool.UnsupportedVideoEncoder", "video.encoder", "Validation.Tool.UnsupportedVideoEncoder", video.Encoder);
        }
        else if (IsHardwareEncoder(video.Encoder) && !(hardwareAvailability ?? []).Any(availability =>
                     availability.IsAvailable && string.Equals(availability.EncoderName, video.Encoder, StringComparison.Ordinal)))
        {
            AddError(issues, "Tool.HardwareEncoderUnavailable", "video.encoder", "Validation.Tool.HardwareEncoderUnavailable", video.Encoder);
        }
        else if (format is not null && !format.VideoEncoders.Contains(video.Encoder, StringComparer.Ordinal))
        {
            AddError(issues, "Profile.VideoEncoderIncompatible", "video.encoder", "Validation.Profile.VideoEncoderIncompatible", video.Encoder);
        }

        switch (video.QualityMode)
        {
            case VideoQualityMode.ConstantQuality when video.ConstantQuality is null or < 0:
                AddError(issues, "Video.QualityRequired", "video.constantQuality", "Validation.Video.QualityRequired");
                break;
            case VideoQualityMode.ConstantQuality when video.BitrateKbps is not null:
                AddError(issues, "Video.QualityAndBitrateExclusive", "video", "Validation.Video.QualityAndBitrateExclusive");
                break;
            case VideoQualityMode.TargetBitrate when video.BitrateKbps is null or <= 0:
                AddError(issues, "Video.BitrateRequired", "video.bitrateKbps", "Validation.Video.BitrateRequired");
                break;
            case VideoQualityMode.TargetBitrate when video.ConstantQuality is not null:
                AddError(issues, "Video.QualityAndBitrateExclusive", "video", "Validation.Video.QualityAndBitrateExclusive");
                break;
            case null:
                AddError(issues, "Video.QualityModeRequired", "video.qualityMode", "Validation.Video.QualityModeRequired");
                break;
        }

        if (video.TwoPass && video.QualityMode != VideoQualityMode.TargetBitrate)
        {
            AddError(issues, "Video.TwoPassRequiresBitrate", "video.twoPass", "Validation.Video.TwoPassRequiresBitrate");
        }

        if (video.TwoPass && IsHardwareEncoder(video.Encoder))
        {
            AddError(issues, "Video.TwoPassUnsupportedForHardware", "video.twoPass", "Validation.Video.TwoPassUnsupportedForHardware");
        }

        if (video.TwoPass && !SupportsTwoPass(video.Encoder))
        {
            AddError(issues, "Video.TwoPassEncoderUnsupported", "video.twoPass", "Validation.Video.TwoPassEncoderUnsupported", video.Encoder);
        }
    }

    private static void ValidateAudio(
        ConversionProfile profile,
        FfmpegCapabilities capabilities,
        ConversionFormatOption? format,
        bool hasAudio,
        List<ConversionValidationIssue> issues)
    {
        var audio = profile.Audio;
        if (audio.Mode != StreamProcessingMode.Exclude && !hasAudio)
        {
            AddError(issues, "Source.AudioMissing", "audio.mode", "Validation.Source.AudioMissing");
        }

        if (audio.Mode == StreamProcessingMode.Copy)
        {
            if (audio.Encoder is not null || audio.SampleRate is not null || audio.Channels is not null || audio.BitrateKbps is not null)
            {
                AddError(issues, "Audio.CopyHasEncodingOptions", "audio", "Validation.Audio.CopyHasEncodingOptions");
            }
            return;
        }

        if (audio.Mode != StreamProcessingMode.Encode)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(audio.Encoder) || !capabilities.Encoders.Contains(audio.Encoder, StringComparer.Ordinal))
        {
            AddError(issues, "Tool.UnsupportedAudioEncoder", "audio.encoder", "Validation.Tool.UnsupportedAudioEncoder", audio.Encoder);
        }
        else if (format is not null && !format.AudioEncoders.Contains(audio.Encoder, StringComparer.Ordinal))
        {
            AddError(issues, "Profile.AudioEncoderIncompatible", "audio.encoder", "Validation.Profile.AudioEncoderIncompatible", audio.Encoder);
        }

        if (audio.SampleRate is <= 0)
        {
            AddError(issues, "Audio.InvalidSampleRate", "audio.sampleRate", "Validation.Audio.InvalidSampleRate");
        }

        if (audio.Channels is <= 0)
        {
            AddError(issues, "Audio.InvalidChannels", "audio.channels", "Validation.Audio.InvalidChannels");
        }

        if (audio.BitrateKbps is <= 0)
        {
            AddError(issues, "Audio.InvalidBitrate", "audio.bitrateKbps", "Validation.Audio.InvalidBitrate");
        }

        if (audio.BitrateKbps is not null && IsLosslessAudioEncoder(audio.Encoder))
        {
            AddError(issues, "Audio.LosslessHasBitrate", "audio.bitrateKbps", "Validation.Audio.LosslessHasBitrate");
        }
    }

    private static void ValidateFilters(MediaSourceInfo source, ConversionProfile profile, List<ConversionValidationIssue> issues)
    {
        var filters = profile.Filters;
        var hasFilters = filters.Crop is not null || filters.Scale is not null || filters.Rotation != VideoRotation.None || filters.Padding is not null;
        if (hasFilters && profile.Video.Mode != StreamProcessingMode.Encode)
        {
            AddError(issues, "Video.FiltersRequireEncoding", "filters", "Validation.Video.FiltersRequireEncoding");
        }

        var videoStream = source.Streams.FirstOrDefault(stream => stream.Type == MediaStreamType.Video);
        if (hasFilters && (videoStream?.Width is null || videoStream.Height is null))
        {
            AddError(issues, "Video.SourceDimensionsRequired", "source.video", "Validation.Video.SourceDimensionsRequired");
            return;
        }

        if (videoStream?.Width is not null && videoStream.Height is not null)
        {
            var dimensions = VideoDimensionCalculator.Calculate(new FrameSize(videoStream.Width.Value, videoStream.Height.Value), filters);
            foreach (var issue in dimensions.Issues)
            {
                AddError(issues, issue.Code, issue.Field, $"Validation.{issue.Code}");
            }

            if (dimensions.OutputSize is { } outputSize && profile.Video.Mode == StreamProcessingMode.Encode &&
                RequiresEvenDimensions(profile.Video.Encoder) && (outputSize.Width % 2 != 0 || outputSize.Height % 2 != 0))
            {
                AddError(issues, "Video.EvenDimensionsRequired", "filters", "Validation.Video.EvenDimensionsRequired");
            }
        }
    }

    private static void ValidateTimeRange(TimeRange? range, List<ConversionValidationIssue> issues)
    {
        if (range is null) return;
        if (range.Start < TimeSpan.Zero)
        {
            AddError(issues, "TimeRange.InvalidStart", "timeRange.start", "Validation.TimeRange.InvalidStart");
        }
        if (range.End is not null && range.End <= range.Start)
        {
            AddError(issues, "TimeRange.InvalidEnd", "timeRange.end", "Validation.TimeRange.InvalidEnd");
        }
    }

    private static void ValidateSubtitle(ConversionProfile profile, List<ConversionValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(profile.ExternalSrtPath)) return;
        if (profile.Video.Mode != StreamProcessingMode.Encode)
        {
            AddError(issues, "Subtitle.BurnInRequiresVideoEncoding", "externalSrtPath", "Validation.Subtitle.BurnInRequiresVideoEncoding");
        }
        if (!string.Equals(Path.GetExtension(profile.ExternalSrtPath), ".srt", StringComparison.OrdinalIgnoreCase))
        {
            AddError(issues, "Subtitle.UnsupportedFile", "externalSrtPath", "Validation.Subtitle.UnsupportedFile");
        }
        else if (!File.Exists(profile.ExternalSrtPath))
        {
            AddError(issues, "Subtitle.FileNotFound", "externalSrtPath", "Validation.Subtitle.FileNotFound", profile.ExternalSrtPath);
        }
    }

    private static bool IsLosslessAudioEncoder(string? encoder) =>
        string.Equals(encoder, "flac", StringComparison.Ordinal) ||
        string.Equals(encoder, "alac", StringComparison.Ordinal) ||
        encoder?.StartsWith("pcm_", StringComparison.Ordinal) == true;

    private static bool IsHardwareEncoder(string? encoder) =>
        encoder?.EndsWith("_nvenc", StringComparison.Ordinal) == true ||
        encoder?.EndsWith("_qsv", StringComparison.Ordinal) == true ||
        encoder?.EndsWith("_amf", StringComparison.Ordinal) == true;

    private static bool SupportsTwoPass(string? encoder) =>
        string.Equals(encoder, "libx264", StringComparison.Ordinal) ||
        string.Equals(encoder, "libx265", StringComparison.Ordinal) ||
        string.Equals(encoder, "libvpx", StringComparison.Ordinal) ||
        string.Equals(encoder, "libvpx-vp9", StringComparison.Ordinal) ||
        string.Equals(encoder, "libaom-av1", StringComparison.Ordinal);

    private static bool RequiresEvenDimensions(string? encoder) =>
        string.Equals(encoder, "libx264", StringComparison.Ordinal) ||
        string.Equals(encoder, "libx265", StringComparison.Ordinal) ||
        IsHardwareEncoder(encoder);

    private static void AddError(List<ConversionValidationIssue> issues, string code, string field, string messageKey, string? detail = null) =>
        issues.Add(new ConversionValidationIssue(code, field, ValidationSeverity.Error, messageKey, detail));
}
