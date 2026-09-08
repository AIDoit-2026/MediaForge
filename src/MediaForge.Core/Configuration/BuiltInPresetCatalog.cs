namespace MediaForge.Core.Configuration;

public static class BuiltInPresetCatalog
{
    public static IReadOnlyList<PresetDocument> All { get; } =
    [
        new PresetDocument(
            PresetDocument.CurrentSchemaVersion,
            Guid.Parse("0f1a55e6-4412-4ef7-a2fe-03d6d3483a01"),
            "YouTube 1080p",
            new ConversionParameterSnapshot("mp4", new Dictionary<string, string>
            {
                ["videoEncoder"] = "libx264",
                ["audioEncoder"] = "aac",
                ["resolution"] = "1920x1080",
                ["qualityMode"] = "crf",
                ["crf"] = "23"
            })),
        new PresetDocument(
            PresetDocument.CurrentSchemaVersion,
            Guid.Parse("8cdb5221-4c1d-414a-bca1-fb77ef82f902"),
            "高质量归档",
            new ConversionParameterSnapshot("mkv", new Dictionary<string, string>
            {
                ["videoEncoder"] = "ffv1",
                ["audioEncoder"] = "flac"
            })),
        new PresetDocument(
            PresetDocument.CurrentSchemaVersion,
            Guid.Parse("ffd81d8f-c822-4ba0-b018-51e17770cc03"),
            "仅音频",
            new ConversionParameterSnapshot("m4a", new Dictionary<string, string>
            {
                ["videoMode"] = "none",
                ["audioEncoder"] = "aac",
                ["audioBitrateKbps"] = "192"
            }))
    ];
}
