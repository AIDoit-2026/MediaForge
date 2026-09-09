namespace MediaForge.Core.Configuration;

public interface IQueueStore
{
    Task<QueueDocument> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(QueueDocument queue, CancellationToken cancellationToken = default);
}
