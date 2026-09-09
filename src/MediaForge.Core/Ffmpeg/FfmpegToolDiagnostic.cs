namespace MediaForge.Core.Ffmpeg;

public sealed record FfmpegToolDiagnostic(
    string Code,
    string ChineseMessage,
    string EnglishMessage,
    string ChineseSuggestedAction,
    string EnglishSuggestedAction,
    string? TechnicalDetails = null);
