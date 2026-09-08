namespace MediaForge.Core.Configuration;

public sealed record PresetDocument(
    int SchemaVersion,
    Guid Id,
    string Name,
    ConversionParameterSnapshot Parameters)
{
    public const int CurrentSchemaVersion = 1;
}
