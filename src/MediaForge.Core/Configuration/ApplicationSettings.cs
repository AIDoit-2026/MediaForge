namespace MediaForge.Core.Configuration;

public sealed record ApplicationSettings(
    int SchemaVersion,
    ApplicationLanguage Language,
    ApplicationTheme Theme,
    string? FfmpegDirectory,
    string? DefaultOutputDirectory,
    OutputConflictPolicy OutputConflictPolicy,
    int MaxConcurrentJobs,
    FolderImportSettings LastFolderImport)
{
    public const int CurrentSchemaVersion = 1;

    public static ApplicationSettings CreateDefault() => new(
        SchemaVersion: CurrentSchemaVersion,
        Language: ApplicationLanguage.System,
        Theme: ApplicationTheme.System,
        FfmpegDirectory: null,
        DefaultOutputDirectory: null,
        OutputConflictPolicy: OutputConflictPolicy.AutoRename,
        MaxConcurrentJobs: 1,
        LastFolderImport: FolderImportSettings.CreateDefault());
}
