namespace MediaForge.Core.Conversion;

public sealed record AudioEncodingSettings(
    StreamProcessingMode Mode,
    string? Encoder,
    int? SampleRate,
    int? Channels,
    int? BitrateKbps)
{
    public static AudioEncodingSettings EncodeWith(string encoder) => new(
        StreamProcessingMode.Encode, encoder, null, null, null);

    public static AudioEncodingSettings Copy { get; } = new(
        StreamProcessingMode.Copy, null, null, null, null);

    public static AudioEncodingSettings Exclude { get; } = new(
        StreamProcessingMode.Exclude, null, null, null, null);
}
