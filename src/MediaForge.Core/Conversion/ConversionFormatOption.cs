namespace MediaForge.Core.Conversion;

public sealed record ConversionFormatOption(
    string Id,
    string DisplayName,
    string FileExtension,
    IReadOnlyList<string> RequiredMuxers,
    IReadOnlyList<string> VideoEncoders,
    IReadOnlyList<string> AudioEncoders);
