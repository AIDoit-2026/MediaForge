using MediaForge.Core.Ffmpeg;

namespace MediaForge.Infrastructure.Ffmpeg;

public sealed record FfmpegCapabilitiesCacheDocument(
    int SchemaVersion,
    string FfmpegPath,
    long FfmpegLastWriteTimeUtcTicks,
    FfmpegCapabilities Capabilities)
{
    public const int CurrentSchemaVersion = 1;
}
