using MediaForge.Core.Configuration;
using MediaForge.Infrastructure.Configuration;

namespace MediaForge.IntegrationTests;

public sealed class DebouncedQueueStoreTests
{
    [Fact]
    public async Task Schedule_persists_only_the_last_queue_snapshot()
    {
        var store = new RecordingStore();
        using var debounced = new DebouncedQueueStore(store, TimeSpan.FromMilliseconds(30));
        var first = QueueDocument.CreateEmpty();
        var last = new QueueDocument(QueueDocument.CurrentSchemaVersion, []);

        debounced.Schedule(first);
        debounced.Schedule(last);
        await Task.Delay(100);

        Assert.Single(store.Saved);
        Assert.Same(last, store.Saved[0]);
    }

    private sealed class RecordingStore : IQueueStore
    {
        public List<QueueDocument> Saved { get; } = [];
        public Task<QueueDocument> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(QueueDocument.CreateEmpty());
        public Task SaveAsync(QueueDocument queue, CancellationToken cancellationToken = default) { Saved.Add(queue); return Task.CompletedTask; }
    }
}
