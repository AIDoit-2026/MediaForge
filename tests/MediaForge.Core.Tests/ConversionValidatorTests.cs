using MediaForge.Core.Conversion;
using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Media;

namespace MediaForge.Core.Tests;

public sealed class ConversionValidatorTests
{
    private static readonly FfmpegCapabilities Capabilities = new(
        Encoders: ["aac", "flac", "h264_nvenc", "libx264", "libx265", "libopus", "pcm_s16le"],
        Decoders: [], Muxers: ["flac", "ipod", "matroska", "mp4", "opus", "wav", "webm"], Demuxers: [], Filters: [], HardwareAccelerations: []);

    [Fact]
    public void Valid_profile_with_available_software_encoders_has_no_errors()
    {
        var result = new ConversionValidator().Validate(VideoAudioSource(), ConversionProfile.CreateDefault(), Capabilities);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Unsupported_selected_encoder_is_a_field_error_that_requests_a_supported_toolset()
    {
        var profile = ConversionProfile.CreateDefault() with { Video = VideoEncodingSettings.EncodeWith("libvpx-vp9") };

        var result = new ConversionValidator().Validate(VideoAudioSource(), profile, Capabilities);

        var issue = Assert.Single(result.Errors);
        Assert.Equal("Tool.UnsupportedVideoEncoder", issue.Code);
        Assert.Equal("video.encoder", issue.Field);
        Assert.Equal("Validation.Tool.UnsupportedVideoEncoder", issue.MessageKey);
    }

    [Fact]
    public void Copy_and_two_pass_constraints_are_reported_together()
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            Video = new VideoEncodingSettings(StreamProcessingMode.Copy, "libx264", VideoQualityMode.ConstantQuality, 23, null, null, true),
            Filters = new VideoFilterSettings(null, new FrameSize(1920, 1080), VideoRotation.None, null)
        };

        var result = new ConversionValidator().Validate(VideoAudioSource(), profile, Capabilities);

        Assert.Contains(result.Errors, issue => issue.Code == "Video.CopyHasEncodingOptions");
        Assert.Contains(result.Errors, issue => issue.Code == "Video.FiltersRequireEncoding");
    }

    [Fact]
    public void Quality_bitrate_two_pass_and_lossless_audio_rules_have_field_errors()
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            Video = new VideoEncodingSettings(StreamProcessingMode.Encode, "h264_nvenc", VideoQualityMode.ConstantQuality, 20, 1200, null, true),
            Audio = new AudioEncodingSettings(StreamProcessingMode.Encode, "flac", null, null, 320)
        };
        var hardware = new[] { new HardwareEncoderAvailability(HardwareEncoderKind.NvidiaNvenc, "h264_nvenc", true, true, null) };

        var result = new ConversionValidator().Validate(VideoAudioSource(), profile, Capabilities, hardware);

        Assert.Contains(result.Errors, issue => issue.Code == "Video.QualityAndBitrateExclusive");
        Assert.Contains(result.Errors, issue => issue.Code == "Video.TwoPassRequiresBitrate");
        Assert.Contains(result.Errors, issue => issue.Code == "Video.TwoPassUnsupportedForHardware");
        Assert.Contains(result.Errors, issue => issue.Code == "Audio.LosslessHasBitrate");
    }

    [Fact]
    public void Audio_only_profile_rejects_missing_audio_and_reports_unknown_duration_as_warning()
    {
        var source = new MediaSourceInfo("input.mp4", "mp4", null, null, null,
            [new MediaStreamInfo(0, MediaStreamType.Video, "h264", null, null, true)]);
        var profile = ConversionProfile.CreateDefault() with
        {
            OutputContainer = "m4a",
            Video = VideoEncodingSettings.Exclude
        };

        var result = new ConversionValidator().Validate(source, profile, Capabilities);

        Assert.Contains(result.Errors, issue => issue.Code == "Source.AudioMissing" && issue.Field == "audio.mode");
        Assert.Contains(result.Warnings, issue => issue.Code == "Source.DurationUnknown");
    }

    [Fact]
    public void Declared_but_unavailable_hardware_encoder_is_rejected_before_queueing()
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            Video = new VideoEncodingSettings(StreamProcessingMode.Encode, "h264_nvenc", VideoQualityMode.TargetBitrate, null, 2000, null, false)
        };
        var hardware = new[] { new HardwareEncoderAvailability(HardwareEncoderKind.NvidiaNvenc, "h264_nvenc", true, false, "Driver error") };

        var result = new ConversionValidator().Validate(VideoAudioSource(), profile, Capabilities, hardware);

        Assert.Contains(result.Errors, issue => issue.Code == "Tool.HardwareEncoderUnavailable" && issue.Field == "video.encoder");
    }

    [Fact]
    public void H264_profile_rejects_odd_final_dimensions()
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            Filters = new VideoFilterSettings(null, new FrameSize(1279, 720), VideoRotation.None, null)
        };

        var result = new ConversionValidator().Validate(VideoAudioSource(), profile, Capabilities);

        Assert.Contains(result.Errors, issue => issue.Code == "Video.EvenDimensionsRequired" && issue.Field == "filters");
    }

    private static MediaSourceInfo VideoAudioSource() => new(
        "input.mp4", "mp4", TimeSpan.FromMinutes(1), null, null,
        [
            new MediaStreamInfo(0, MediaStreamType.Video, "h264", null, null, true, Width: 1920, Height: 1080),
            new MediaStreamInfo(1, MediaStreamType.Audio, "aac", null, null, true)
        ]);
}
