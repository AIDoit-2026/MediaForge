using MediaForge.Core.Importing;

namespace MediaForge.Core.Configuration;

public sealed record FolderImportSettings(
    string? Directory,
    bool IncludeSubdirectories,
    FileNameFilterMode FilterMode,
    string FilterExpression)
{
    public static FolderImportSettings CreateDefault() => new(
        Directory: null,
        IncludeSubdirectories: true,
        FilterMode: FileNameFilterMode.All,
        FilterExpression: string.Empty);
}
