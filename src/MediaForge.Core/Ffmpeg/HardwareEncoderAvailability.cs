namespace MediaForge.Core.Ffmpeg;

public sealed record HardwareEncoderAvailability(
    HardwareEncoderKind Kind,
    string EncoderName,
    bool IsDeclared,
    bool IsAvailable,
    string? Diagnostic);
