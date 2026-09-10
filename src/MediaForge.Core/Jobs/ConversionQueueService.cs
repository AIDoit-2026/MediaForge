using MediaForge.Core.Configuration;

namespace MediaForge.Core.Jobs;

/// <summary>
/// Owns queue ordering and all legal conversion job transitions. UI code observes immutable snapshots only.
/// </summary>
public sealed class ConversionQueueService
{
    private readonly object _gate = new();
    private readonly List<ConversionJob> _jobs = [];

    public event EventHandler<ConversionQueueChangedEventArgs>? Changed;

    public ConversionQueueSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return CreateSnapshot();
        }
    }

    public ConversionJobSnapshot Add(string inputPath, string outputPath, ConversionParameterSnapshot parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(parameters);

        ConversionJobSnapshot added;
        ConversionQueueSnapshot snapshot;
        lock (_gate)
        {
            var job = new ConversionJob(Guid.NewGuid(), inputPath, outputPath, ConversionJobStatus.Ready, parameters);
            _jobs.Add(job);
            added = ConversionJobSnapshot.From(job);
            snapshot = CreateSnapshot();
        }
        RaiseChanged(snapshot);
        return added;
    }

    public void Remove(Guid jobId)
    {
        ConversionQueueSnapshot snapshot;
        lock (_gate)
        {
            var job = Find(jobId);
            EnsureNotRunning(job, "remove");
            _jobs.Remove(job);
            snapshot = CreateSnapshot();
        }
        RaiseChanged(snapshot);
    }

    public void Move(Guid jobId, int targetIndex)
    {
        ConversionQueueSnapshot snapshot;
        lock (_gate)
        {
            if (targetIndex < 0 || targetIndex >= _jobs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(targetIndex));
            }

            var job = Find(jobId);
            EnsureNotRunning(job, "reorder");
            var sourceIndex = _jobs.IndexOf(job);
            _jobs.RemoveAt(sourceIndex);
            _jobs.Insert(targetIndex, job);
            snapshot = CreateSnapshot();
        }
        RaiseChanged(snapshot);
    }

    public void Clear()
    {
        ConversionQueueSnapshot snapshot;
        lock (_gate)
        {
            if (_jobs.Any(job => job.Status == ConversionJobStatus.Running))
            {
                throw new InvalidOperationException("Cannot clear the queue while a conversion is running.");
            }

            _jobs.Clear();
            snapshot = CreateSnapshot();
        }
        RaiseChanged(snapshot);
    }

    public void Queue(Guid jobId) => Transition(jobId, job => job.Queue());

    public void Start(Guid jobId) => Transition(jobId, job => job.Start());

    public void Complete(Guid jobId) => Transition(jobId, job => job.Complete());

    public void Skip(Guid jobId) => Transition(jobId, job => job.Skip());

    public void Fail(Guid jobId) => Transition(jobId, job => job.Fail());

    public void Interrupt(Guid jobId) => Transition(jobId, job => job.Interrupt());

    public void Retry(Guid jobId) => Transition(jobId, job =>
    {
        if (job.Status is not (ConversionJobStatus.Failed or ConversionJobStatus.Interrupted))
        {
            throw new InvalidOperationException("Only failed or interrupted jobs can be retried.");
        }
        job.Queue();
    });

    public void Restore(QueueDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        ConversionQueueSnapshot snapshot;
        lock (_gate)
        {
            _jobs.Clear();
            foreach (var persisted in document.RestoreAfterApplicationStart().Jobs)
            {
                _jobs.Add(new ConversionJob(
                    persisted.Id,
                    persisted.InputPath,
                    persisted.OutputPath,
                    ToRuntimeStatus(persisted.Status),
                    persisted.Parameters));
            }
            snapshot = CreateSnapshot();
        }
        RaiseChanged(snapshot);
    }

    public QueueDocument CreateDocument()
    {
        lock (_gate)
        {
            return new QueueDocument(
                QueueDocument.CurrentSchemaVersion,
                _jobs.Select(job => new PersistedConversionJob(
                    job.Id,
                    job.InputPath,
                    job.OutputPath,
                    ToPersistedStatus(job.Status),
                    job.Parameters)).ToArray());
        }
    }

    private void Transition(Guid jobId, Action<ConversionJob> transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        ConversionQueueSnapshot snapshot;
        lock (_gate)
        {
            transition(Find(jobId));
            snapshot = CreateSnapshot();
        }
        RaiseChanged(snapshot);
    }

    private ConversionJob Find(Guid jobId) =>
        _jobs.SingleOrDefault(job => job.Id == jobId)
        ?? throw new KeyNotFoundException($"The conversion job '{jobId}' was not found.");

    private ConversionQueueSnapshot CreateSnapshot() =>
        new(_jobs.Select(ConversionJobSnapshot.From).ToArray());

    private void RaiseChanged(ConversionQueueSnapshot snapshot) =>
        Changed?.Invoke(this, new ConversionQueueChangedEventArgs(snapshot));

    private static void EnsureNotRunning(ConversionJob job, string operation)
    {
        if (job.Status == ConversionJobStatus.Running)
        {
            throw new InvalidOperationException($"Cannot {operation} a running conversion job.");
        }
    }

    private static ConversionJobStatus ToRuntimeStatus(PersistedJobStatus status) => status switch
    {
        PersistedJobStatus.Ready => ConversionJobStatus.Ready,
        PersistedJobStatus.Queued => ConversionJobStatus.Queued,
        PersistedJobStatus.Running => ConversionJobStatus.Running,
        PersistedJobStatus.Succeeded => ConversionJobStatus.Succeeded,
        PersistedJobStatus.Failed => ConversionJobStatus.Failed,
        PersistedJobStatus.Interrupted => ConversionJobStatus.Interrupted,
        PersistedJobStatus.Skipped => ConversionJobStatus.Skipped,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static PersistedJobStatus ToPersistedStatus(ConversionJobStatus status) => status switch
    {
        ConversionJobStatus.Ready => PersistedJobStatus.Ready,
        ConversionJobStatus.Queued => PersistedJobStatus.Queued,
        ConversionJobStatus.Running => PersistedJobStatus.Running,
        ConversionJobStatus.Succeeded => PersistedJobStatus.Succeeded,
        ConversionJobStatus.Failed => PersistedJobStatus.Failed,
        ConversionJobStatus.Interrupted => PersistedJobStatus.Interrupted,
        ConversionJobStatus.Skipped => PersistedJobStatus.Skipped,
        ConversionJobStatus.Probing or ConversionJobStatus.Invalid => throw new InvalidOperationException(
            $"The transient job status '{status}' cannot be persisted."),
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
