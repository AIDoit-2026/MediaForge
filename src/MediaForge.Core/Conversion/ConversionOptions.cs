namespace MediaForge.Core.Conversion;

public sealed record ConversionOptions(
    IReadOnlyList<ConversionFormatOption> Formats,
    IReadOnlyList<string> AvailableVideoEncoders,
    IReadOnlyList<string> AvailableAudioEncoders);
