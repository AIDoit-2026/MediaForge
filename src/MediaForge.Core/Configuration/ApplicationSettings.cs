namespace MediaForge.Core.Configuration;

public sealed record ApplicationSettings(
    int SchemaVersion,
    ApplicationLanguage Language,
    ApplicationTheme Theme,
    string? FfmpegDirectory,
    string? DefaultOutputDirectory,
    OutputConflictPolicy OutputConflictPolicy,
    int MaxConcurrentJobs,
    FolderImportSettings LastFolderImport,
    int WindowWidth = 1680,
    int WindowHeight = 800,
    OutputDirectoryMode OutputDirectoryMode = OutputDirectoryMode.SourceSiblingOutputDirectory,
    string? MainPageOutputDirectory = null)
{
    public const int CurrentSchemaVersion = 2;

    public static ApplicationSettings CreateDefault() => new(
        SchemaVersion: CurrentSchemaVersion,
        Language: ApplicationLanguage.System,
        Theme: ApplicationTheme.System,
        FfmpegDirectory: null,
        DefaultOutputDirectory: null,
        OutputConflictPolicy: OutputConflictPolicy.AutoRename,
        MaxConcurrentJobs: 1,
        LastFolderImport: FolderImportSettings.CreateDefault(),
        WindowWidth: 1680,
        WindowHeight: 800,
        OutputDirectoryMode: OutputDirectoryMode.SourceSiblingOutputDirectory,
        MainPageOutputDirectory: null);
}
