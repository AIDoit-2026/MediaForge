using MediaForge.Core.Importing;
using MediaForge.Core.Media;
using MediaForge.Infrastructure.Importing;

namespace MediaForge.IntegrationTests;

public sealed class FolderMediaImportServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "MediaForge.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Import_recurses_filters_and_keeps_other_items_when_one_probe_fails()
    {
        Directory.CreateDirectory(Path.Combine(_root, "nested"));
        await File.WriteAllTextAsync(Path.Combine(_root, "keep.mp4"), "x");
        await File.WriteAllTextAsync(Path.Combine(_root, "skip.txt"), "x");
        await File.WriteAllTextAsync(Path.Combine(_root, "nested", "broken.mp4"), "x");
        var service = new FolderMediaImportService(new FakeProbe());

        var items = await service.ImportFolderAsync(
            _root,
            includeSubdirectories: true,
            FileNameFilter.Create(FileNameFilterMode.Wildcard, "*.mp4"),
            maxConcurrency: 2);

        Assert.Equal(2, items.Count);
        Assert.Contains(items, item => item.Path.EndsWith("keep.mp4", StringComparison.OrdinalIgnoreCase) && item.Media is not null);
        Assert.Contains(items, item => item.Path.EndsWith("broken.mp4", StringComparison.OrdinalIgnoreCase) && item.Error is not null);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private sealed class FakeProbe : IMediaProbeService
    {
        public Task<MediaSourceInfo> ProbeAsync(string path, CancellationToken cancellationToken = default) =>
            path.Contains("broken", StringComparison.OrdinalIgnoreCase)
                ? throw new InvalidOperationException("Broken media.")
                : Task.FromResult(new MediaSourceInfo(path, "mp4", null, null, null, []));
    }
}
