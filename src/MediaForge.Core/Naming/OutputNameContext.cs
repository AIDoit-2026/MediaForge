namespace MediaForge.Core.Naming;

public sealed record OutputNameContext(
    string Name,
    string Extension,
    string Preset,
    int Index,
    DateTimeOffset CreatedAt,
    int Width,
    int Height,
    string VideoCodec,
    string AudioCodec);
