using MediaForge.Core.Configuration;
using MediaForge.Core.Conversion;
using MediaForge.Core.Media;
using MediaForge.Infrastructure.Configuration;

namespace MediaForge.IntegrationTests;

public sealed class JobManifestStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "MediaForge.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Store_round_trips_and_deletes_a_manifest_by_its_exact_job_identifier()
    {
        var paths = new Paths(_root);
        var store = new JobManifestStore(paths, new AtomicJsonFileStore());
        var manifest = JobManifest.Create(new ConversionJobSpec(
            new MediaSourceInfo("in.mp4", "mp4", null, null, null, []),
            Path.Combine(_root, "out.mp4"),
            Path.Combine(_root, ".out.id.mediaforge.tmp.mp4"),
            ConversionProfile.CreateDefault(),
            DateTimeOffset.UtcNow,
            JobId: Guid.NewGuid()), []);

        await store.SaveAsync(manifest);
        var loaded = Assert.Single(await store.LoadAsync());
        await store.DeleteAsync(manifest.JobId);

        Assert.Equal(manifest.JobId, loaded.JobId);
        Assert.Empty(await store.LoadAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private sealed class Paths(string root) : IApplicationPaths
    {
        public string BaseDirectory => root;
        public string ConfigDirectory => Path.Combine(root, "config");
        public bool CanPersist => true;
        public string? PersistenceWarningCode => null;
    }
}
