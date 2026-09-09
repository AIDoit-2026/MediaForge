namespace MediaForge.Core.Importing;

public interface IMediaImportService
{
    Task<IReadOnlyList<MediaImportItem>> ImportFolderAsync(
        string directory,
        bool includeSubdirectories,
        FileNameFilter filter,
        int maxConcurrency,
        CancellationToken cancellationToken = default);
}
