using MediaForge.Core.Configuration;
using MediaForge.Infrastructure.Configuration;

namespace MediaForge.IntegrationTests;

public sealed class QueueStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "MediaForge.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Load_restores_running_jobs_as_interrupted()
    {
        var store = new QueueStore(new Paths(_root), new AtomicJsonFileStore());
        var queue = new QueueDocument(QueueDocument.CurrentSchemaVersion,
        [new PersistedConversionJob(Guid.NewGuid(), "in.mp4", "out.mp4", PersistedJobStatus.Running, ConversionParameterSnapshot.CreateDefault())]);
        await store.SaveAsync(queue);
        var loaded = await store.LoadAsync();
        Assert.Equal(PersistedJobStatus.Interrupted, loaded.Jobs[0].Status);
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }

    private sealed class Paths(string root) : IApplicationPaths
    {
        public string BaseDirectory => root;
        public string ConfigDirectory => Path.Combine(root, "config");
        public bool CanPersist => true;
        public string? PersistenceWarningCode => null;
    }
}
