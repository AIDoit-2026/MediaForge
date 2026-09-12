using MediaForge.Core.Configuration;
using MediaForge.Core.Jobs;

namespace MediaForge.Core.Tests;

public sealed class ConversionQueueSchedulerTests
{
    [Fact]
    public async Task Scheduler_respects_the_initial_concurrency_limit_then_starts_more_jobs_when_raised()
    {
        var queue = CreateQueuedJobs(3);
        var started = new List<Guid>();
        var gates = new Dictionary<Guid, TaskCompletionSource>();
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scheduler = new ConversionQueueScheduler(queue, async (job, _) =>
        {
            lock (started)
            {
                started.Add(job.Id);
                if (started.Count >= 2) startSignal.TrySetResult();
            }
            await gates[job.Id].Task;
        });
        foreach (var job in queue.GetSnapshot().Jobs)
        {
            gates[job.Id] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        var drain = scheduler.StartQueuedJobsAsync();
        await WaitUntilAsync(() => Count(started) == 1);
        scheduler.SetMaximumConcurrency(2);
        await startSignal.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(2, Count(started));
        foreach (var gate in gates.Values) gate.TrySetResult();
        await drain.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(3, Count(started));
        Assert.All(queue.GetSnapshot().Jobs, job => Assert.Equal(ConversionJobStatus.Succeeded, job.Status));
    }

    [Fact]
    public async Task Lowering_the_limit_does_not_preempt_running_jobs_and_applies_to_future_starts()
    {
        var queue = CreateQueuedJobs(3);
        var started = new List<Guid>();
        var gates = new Dictionary<Guid, TaskCompletionSource>();
        var scheduler = new ConversionQueueScheduler(queue, async (job, _) =>
        {
            lock (started) started.Add(job.Id);
            await gates[job.Id].Task;
        }, maximumConcurrency: 2);
        foreach (var job in queue.GetSnapshot().Jobs)
        {
            gates[job.Id] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        var drain = scheduler.StartQueuedJobsAsync();
        await WaitUntilAsync(() => Count(started) == 2);
        scheduler.SetMaximumConcurrency(1);

        Assert.Equal(2, queue.GetSnapshot().Jobs.Count(job => job.Status == ConversionJobStatus.Running));
        foreach (var id in StartedIds(started)) gates[id].TrySetResult();
        await WaitUntilAsync(() => Count(started) == 3);
        Assert.Equal(1, queue.GetSnapshot().Jobs.Count(job => job.Status == ConversionJobStatus.Running));

        foreach (var gate in gates.Values) gate.TrySetResult();
        await drain.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Executor_failure_marks_only_that_job_failed_and_keeps_other_queued_jobs_running()
    {
        var queue = CreateQueuedJobs(2);
        var invocation = 0;
        var scheduler = new ConversionQueueScheduler(queue, (_, _) =>
        {
            invocation++;
            return invocation == 1 ? Task.FromException(new InvalidOperationException("ffmpeg failed")) : Task.CompletedTask;
        });

        await scheduler.StartQueuedJobsAsync().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal([ConversionJobStatus.Failed, ConversionJobStatus.Succeeded], queue.GetSnapshot().Jobs.Select(job => job.Status));
    }

    [Fact]
    public async Task Executor_skip_marks_only_that_job_skipped()
    {
        var queue = CreateQueuedJobs(2);
        var invocation = 0;
        var scheduler = new ConversionQueueScheduler(queue, (_, _) =>
        {
            invocation++;
            return invocation == 1
                ? Task.FromException(new ConversionSkippedException())
                : Task.CompletedTask;
        });

        await scheduler.StartQueuedJobsAsync().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal([ConversionJobStatus.Skipped, ConversionJobStatus.Succeeded], queue.GetSnapshot().Jobs.Select(job => job.Status));
    }

    [Fact]
    public async Task Stop_cancels_running_work_marks_it_interrupted_and_does_not_start_more_jobs()
    {
        var queue = CreateQueuedJobs(2);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scheduler = new ConversionQueueScheduler(queue, async (_, cancellationToken) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });

        var drain = scheduler.StartQueuedJobsAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await scheduler.StopAsync().WaitAsync(TimeSpan.FromSeconds(2));

        var statuses = queue.GetSnapshot().Jobs.Select(job => job.Status).ToArray();
        Assert.Equal(ConversionJobStatus.Interrupted, statuses[0]);
        Assert.Equal(ConversionJobStatus.Queued, statuses[1]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => scheduler.StartQueuedJobsAsync());
        Assert.False(drain.IsCompleted);
    }

    [Fact]
    public async Task Stop_times_out_when_an_executor_ignores_cancellation()
    {
        var queue = CreateQueuedJobs(1);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var neverCompletes = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scheduler = new ConversionQueueScheduler(
            queue,
            (_, _) =>
            {
                started.TrySetResult();
                return neverCompletes.Task;
            },
            stopTimeout: TimeSpan.FromMilliseconds(50));

        _ = scheduler.StartQueuedJobsAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await Assert.ThrowsAsync<TimeoutException>(() => scheduler.StopAsync());
        neverCompletes.TrySetResult();
    }

    private static ConversionQueueService CreateQueuedJobs(int count)
    {
        var queue = new ConversionQueueService();
        for (var index = 0; index < count; index++)
        {
            var job = queue.Add($"input-{index}.mp4", $"output-{index}.mp4", ConversionParameterSnapshot.CreateDefault());
            queue.Queue(job.Id);
        }
        return queue;
    }

    private static int Count(ICollection<Guid> values)
    {
        lock (values) return values.Count;
    }

    private static IReadOnlyList<Guid> StartedIds(ICollection<Guid> values)
    {
        lock (values) return values.ToArray();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= timeout) throw new TimeoutException("Condition was not reached.");
            await Task.Delay(10);
        }
    }
}
