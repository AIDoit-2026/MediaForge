namespace MediaForge.Core.Conversion;

public sealed record VideoEncodingSettings(
    StreamProcessingMode Mode,
    string? Encoder,
    VideoQualityMode? QualityMode,
    int? ConstantQuality,
    int? BitrateKbps,
    string? Preset,
    bool TwoPass)
{
    public static VideoEncodingSettings EncodeWith(string encoder) => new(
        StreamProcessingMode.Encode,
        encoder,
        VideoQualityMode.ConstantQuality,
        ConstantQuality: 23,
        BitrateKbps: null,
        Preset: null,
        TwoPass: false);

    public static VideoEncodingSettings Copy { get; } = new(
        StreamProcessingMode.Copy, null, null, null, null, null, false);

    public static VideoEncodingSettings Exclude { get; } = new(
        StreamProcessingMode.Exclude, null, null, null, null, null, false);
}
