using MediaForge.Core.Configuration;
using MediaForge.Core.Conversion;
using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Media;
using MediaForge.Core.Naming;

namespace MediaForge.Core.Tests;

public sealed class ConversionLogicParameterizedTests
{
    private static readonly FfmpegCapabilities Capabilities = new(
        Encoders: ["aac", "flac", "libx264", "libx265", "libvpx-vp9", "libopus", "pcm_s16le"],
        Decoders: [],
        Muxers: ["flac", "ipod", "matroska", "mp3", "mp4", "opus", "wav", "webm"],
        Demuxers: [], Filters: [], HardwareAccelerations: []);

    [Theory]
    [InlineData("mp4", "libx264", true)]
    [InlineData("mkv", "libx265", true)]
    [InlineData("webm", "libvpx-vp9", true)]
    [InlineData("webm", "libx264", false)]
    [InlineData("mp4", "libvpx-vp9", false)]
    public void Validator_parameterizes_container_and_video_encoder_compatibility(
        string container,
        string encoder,
        bool isValid)
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            OutputContainer = container,
            Video = VideoEncodingSettings.EncodeWith(encoder),
            Audio = container == "webm" ? AudioEncodingSettings.EncodeWith("libopus") : AudioEncodingSettings.EncodeWith("aac")
        };

        var result = new ConversionValidator().Validate(Source(), profile, Capabilities);

        Assert.Equal(isValid, result.IsValid);
        Assert.Equal(!isValid, result.Errors.Any(issue => issue.Code == "Profile.VideoEncoderIncompatible"));
    }

    [Theory]
    [InlineData("flac", 0, "Audio.InvalidBitrate")]
    [InlineData("flac", 192, "Audio.LosslessHasBitrate")]
    [InlineData("aac", 0, "Audio.InvalidBitrate")]
    [InlineData("aac", 192, null)]
    public void Validator_parameterizes_audio_bitrate_rules(string encoder, int bitrateKbps, string? expectedError)
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            OutputContainer = encoder == "flac" ? "flac" : "mp4",
            Audio = new AudioEncodingSettings(StreamProcessingMode.Encode, encoder, null, null, bitrateKbps)
        };

        var result = new ConversionValidator().Validate(Source(), profile, Capabilities);

        Assert.Equal(expectedError is null, result.Errors.All(issue => issue.Code != expectedError));
        if (expectedError is not null) Assert.Contains(result.Errors, issue => issue.Code == expectedError);
    }

    [Theory]
    [InlineData(VideoRotation.None, 1920, 1080)]
    [InlineData(VideoRotation.Clockwise90, 1080, 1920)]
    [InlineData(VideoRotation.CounterClockwise90, 1080, 1920)]
    [InlineData(VideoRotation.Rotate180, 1920, 1080)]
    public void Dimension_calculator_parameterizes_rotation_output_size(VideoRotation rotation, int width, int height)
    {
        var result = VideoDimensionCalculator.Calculate(
            new FrameSize(1920, 1080),
            new VideoFilterSettings(null, null, rotation, null));

        Assert.Equal(new FrameSize(width, height), result.OutputSize);
    }

    [Theory]
    [InlineData("a:b", "a_b")]
    [InlineData("a/b", "a_b")]
    [InlineData("a?b", "a_b")]
    public void Output_name_template_parameterizes_windows_invalid_character_cleanup(string name, string expected)
    {
        var context = new OutputNameContext(name, "mp4", "Default", 1, DateTimeOffset.UnixEpoch, 1, 1, "libx264", "aac");

        Assert.Equal($"{expected}.mp4", OutputNameTemplate.Render("{name}.{ext}", context));
    }

    private static MediaSourceInfo Source() => new(
        "input.mp4", "mp4", TimeSpan.FromSeconds(1), null, null,
        [
            new MediaStreamInfo(0, MediaStreamType.Video, "h264", null, null, true, Width: 1920, Height: 1080),
            new MediaStreamInfo(1, MediaStreamType.Audio, "aac", null, null, true)
        ]);
}
