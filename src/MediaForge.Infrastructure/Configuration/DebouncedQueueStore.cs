using MediaForge.Core.Configuration;

namespace MediaForge.Infrastructure.Configuration;

public sealed class DebouncedQueueStore : IDisposable
{
    private readonly IQueueStore _store;
    private readonly TimeSpan _delay;
    private readonly object _sync = new();
    private CancellationTokenSource? _pending;

    public DebouncedQueueStore(IQueueStore store, TimeSpan? delay = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _delay = delay ?? TimeSpan.FromMilliseconds(300);
    }

    public void Schedule(QueueDocument queue)
    {
        lock (_sync)
        {
            _pending?.Cancel();
            _pending?.Dispose();
            _pending = new CancellationTokenSource();
            _ = SaveAfterDelayAsync(queue, _pending.Token);
        }
    }

    public async Task FlushAsync(QueueDocument queue, CancellationToken cancellationToken = default)
    {
        lock (_sync) { _pending?.Cancel(); }
        await _store.SaveAsync(queue, cancellationToken);
    }

    public void Dispose()
    {
        lock (_sync) { _pending?.Cancel(); _pending?.Dispose(); _pending = null; }
    }

    private async Task SaveAfterDelayAsync(QueueDocument queue, CancellationToken cancellationToken)
    {
        try { await Task.Delay(_delay, cancellationToken); await _store.SaveAsync(queue, cancellationToken); }
        catch (OperationCanceledException) { }
    }
}
