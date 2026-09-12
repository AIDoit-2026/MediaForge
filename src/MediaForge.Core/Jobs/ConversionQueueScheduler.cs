namespace MediaForge.Core.Jobs;

/// <summary>
/// Starts queued jobs up to a user-selected limit. Changing the limit never preempts jobs already running;
/// it only changes how many future jobs may start.
/// </summary>
public sealed class ConversionQueueScheduler : IConversionQueueScheduler
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);
    private readonly object _gate = new();
    private readonly ConversionQueueService _queue;
    private readonly Func<ConversionJobSnapshot, CancellationToken, Task> _executeAsync;
    private readonly TimeSpan _stopTimeout;
    private readonly HashSet<Guid> _runningJobIds = [];
    private readonly Dictionary<Guid, TaskCompletionSource> _runningCompletions = [];
    private readonly CancellationTokenSource _shutdownCancellation = new();
    private TaskCompletionSource _idleCompletion = CompletedSource();
    private bool _pumpActive;
    private bool _schedulingEnabled;
    private int _maximumConcurrency;

    public ConversionQueueScheduler(
        ConversionQueueService queue,
        Func<ConversionJobSnapshot, CancellationToken, Task> executeAsync,
        int maximumConcurrency = 1,
        TimeSpan? stopTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(executeAsync);
        ValidateConcurrency(maximumConcurrency);
        if (stopTimeout.HasValue && stopTimeout.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(stopTimeout), "The stop timeout must be positive.");
        }
        _queue = queue;
        _executeAsync = executeAsync;
        _maximumConcurrency = maximumConcurrency;
        _stopTimeout = stopTimeout ?? StopTimeout;
    }

    public int MaximumConcurrency
    {
        get { lock (_gate) return _maximumConcurrency; }
    }

    public void SetMaximumConcurrency(int maximumConcurrency)
    {
        ValidateConcurrency(maximumConcurrency);
        lock (_gate)
        {
            _maximumConcurrency = maximumConcurrency;
        }
        RequestPump();
    }

    public Task StartQueuedJobsAsync()
    {
        Task idleTask;
        lock (_gate)
        {
            if (_shutdownCancellation.IsCancellationRequested)
            {
                throw new InvalidOperationException("A stopped queue scheduler cannot be restarted.");
            }
            _schedulingEnabled = true;
            if (_idleCompletion.Task.IsCompleted)
            {
                _idleCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            idleTask = _idleCompletion.Task;
        }
        RequestPump();
        return idleTask;
    }

    public async Task StopAsync()
    {
        Task[] runningTasks;
        lock (_gate)
        {
            _schedulingEnabled = false;
            _shutdownCancellation.Cancel();
            runningTasks = _runningCompletions.Values.Select(completion => completion.Task).ToArray();
        }

        if (runningTasks.Length == 0)
        {
            return;
        }

        await Task.WhenAll(runningTasks).WaitAsync(_stopTimeout).ConfigureAwait(false);
    }

    private void RequestPump()
    {
        lock (_gate)
        {
            if (!_schedulingEnabled || _pumpActive)
            {
                return;
            }
            _pumpActive = true;
        }
        _ = Task.Run(PumpAsync);
    }

    private async Task PumpAsync()
    {
        try
        {
            while (true)
            {
                List<ConversionJobSnapshot> jobsToStart;
                lock (_gate)
                {
                    var availableSlots = _maximumConcurrency - _runningJobIds.Count;
                    if (availableSlots <= 0)
                    {
                        break;
                    }

                    jobsToStart = _queue.GetSnapshot().Jobs
                        .Where(job => job.Status == ConversionJobStatus.Queued && !_runningJobIds.Contains(job.Id))
                        .Take(availableSlots)
                        .ToList();
                    foreach (var job in jobsToStart)
                    {
                        _runningJobIds.Add(job.Id);
                    }
                }

                if (jobsToStart.Count == 0)
                {
                    break;
                }

                foreach (var job in jobsToStart)
                {
                    _queue.Start(job.Id);
                    var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    lock (_gate)
                    {
                        _runningCompletions.Add(job.Id, completion);
                    }
                    _ = Task.Run(() => ExecuteOneAsync(job, completion));
                }
            }
        }
        finally
        {
            bool shouldPumpAgain;
            lock (_gate)
            {
                _pumpActive = false;
                shouldPumpAgain = _schedulingEnabled && _runningJobIds.Count < _maximumConcurrency &&
                    _queue.GetSnapshot().Jobs.Any(job => job.Status == ConversionJobStatus.Queued);
                if (!shouldPumpAgain && _runningJobIds.Count == 0 &&
                    !_queue.GetSnapshot().Jobs.Any(job => job.Status == ConversionJobStatus.Queued))
                {
                    _idleCompletion.TrySetResult();
                }
            }
            if (shouldPumpAgain)
            {
                RequestPump();
            }
        }

        await Task.CompletedTask;
    }

    private async Task ExecuteOneAsync(ConversionJobSnapshot job, TaskCompletionSource completion)
    {
        try
        {
            await _executeAsync(job, _shutdownCancellation.Token);
            _queue.Complete(job.Id);
        }
        catch (ConversionSkippedException)
        {
            _queue.Skip(job.Id);
        }
        catch (OperationCanceledException) when (_shutdownCancellation.IsCancellationRequested)
        {
            _queue.Interrupt(job.Id);
        }
        catch (Exception)
        {
            _queue.Fail(job.Id);
        }
        finally
        {
            lock (_gate)
            {
                _runningJobIds.Remove(job.Id);
                _runningCompletions.Remove(job.Id);
            }
            completion.TrySetResult();
            RequestPump();
        }
    }

    private static void ValidateConcurrency(int maximumConcurrency)
    {
        if (maximumConcurrency is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumConcurrency), "The concurrent conversion limit must be between 1 and 4.");
        }
    }

    private static TaskCompletionSource CompletedSource()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }
}
