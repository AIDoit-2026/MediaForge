namespace MediaForge.Core.Configuration;

/// <summary>
/// Provides the single entry point for upgrading persisted user presets.
/// Add an explicit case here whenever a new preset schema is introduced.
/// </summary>
public static class PresetDocumentMigration
{
    public static PresetDocument MigrateToCurrent(PresetDocument preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        return preset.SchemaVersion switch
        {
            PresetDocument.CurrentSchemaVersion => preset,
            0 => preset with { SchemaVersion = PresetDocument.CurrentSchemaVersion },
            > PresetDocument.CurrentSchemaVersion => throw new NotSupportedException(
                $"Preset '{preset.Name}' uses schema version {preset.SchemaVersion}, which this version of MediaForge cannot read."),
            _ => throw new NotSupportedException(
                $"Preset '{preset.Name}' uses unsupported schema version {preset.SchemaVersion}.")
        };
    }
}
