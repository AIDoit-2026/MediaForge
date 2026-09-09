using MediaForge.Core.Importing;
using MediaForge.Core.Media;

namespace MediaForge.Infrastructure.Importing;

public sealed class FolderMediaImportService : IMediaImportService
{
    private readonly IMediaProbeService _mediaProbeService;

    public FolderMediaImportService(IMediaProbeService mediaProbeService) =>
        _mediaProbeService = mediaProbeService ?? throw new ArgumentNullException(nameof(mediaProbeService));

    public async Task<IReadOnlyList<MediaImportItem>> ImportFolderAsync(
        string directory,
        bool includeSubdirectories,
        FileNameFilter filter,
        int maxConcurrency,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var paths = Directory.EnumerateFiles(directory, "*", searchOption)
            .Where(filter.IsMatch)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        using var gate = new SemaphoreSlim(maxConcurrency);
        var tasks = paths.Select(async path =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return new MediaImportItem(path, await _mediaProbeService.ProbeAsync(path, cancellationToken), null);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                return new MediaImportItem(path, null, error.Message);
            }
            finally { gate.Release(); }
        });
        return await Task.WhenAll(tasks);
    }
}
