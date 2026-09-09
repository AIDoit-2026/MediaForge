using MediaForge.Core.Configuration;

namespace MediaForge.Infrastructure.Configuration;

public sealed class QueueStore : IQueueStore
{
    private readonly IApplicationPaths _paths;
    private readonly AtomicJsonFileStore _store;

    public QueueStore(IApplicationPaths paths, AtomicJsonFileStore store)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<QueueDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_paths.CanPersist) return QueueDocument.CreateEmpty();
        var result = await _store.ReadAsync(Path.Combine(_paths.ConfigDirectory, "queue.json"), QueueDocument.CreateEmpty, cancellationToken);
        return result.Value.RestoreAfterApplicationStart();
    }

    public Task SaveAsync(QueueDocument queue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queue);
        return _paths.CanPersist
            ? _store.WriteAsync(Path.Combine(_paths.ConfigDirectory, "queue.json"), queue, cancellationToken)
            : Task.CompletedTask;
    }
}
