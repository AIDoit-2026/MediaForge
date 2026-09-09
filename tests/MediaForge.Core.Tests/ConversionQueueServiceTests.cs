using MediaForge.Core.Configuration;
using MediaForge.Core.Jobs;

namespace MediaForge.Core.Tests;

public sealed class ConversionQueueServiceTests
{
    [Fact]
    public void Service_exposes_only_snapshots_and_raises_change_for_add_reorder_and_remove()
    {
        var service = new ConversionQueueService();
        var changes = new List<ConversionQueueSnapshot>();
        service.Changed += (_, args) => changes.Add(args.Snapshot);
        var first = service.Add("first.mp4", "first.mkv", new ConversionParameterSnapshot("mkv", new Dictionary<string, string> { ["crf"] = "20" }));
        var second = service.Add("second.mp4", "second.mkv", ConversionParameterSnapshot.CreateDefault());

        service.Move(second.Id, 0);
        service.Remove(first.Id);

        Assert.Equal(4, changes.Count);
        var current = Assert.Single(service.GetSnapshot().Jobs);
        Assert.Equal(second.Id, current.Id);
        Assert.Equal("mp4", current.Parameters.OutputContainer);
    }

    [Fact]
    public void Retry_requeues_interrupted_job_and_persists_its_parameter_snapshot()
    {
        var service = new ConversionQueueService();
        var parameters = new ConversionParameterSnapshot("webm", new Dictionary<string, string> { ["videoEncoder"] = "libvpx-vp9" });
        service.Restore(new QueueDocument(QueueDocument.CurrentSchemaVersion,
            [new PersistedConversionJob(Guid.NewGuid(), "input.mp4", "output.webm", PersistedJobStatus.Interrupted, parameters)]));
        var restored = Assert.Single(service.GetSnapshot().Jobs);

        service.Retry(restored.Id);
        var document = service.CreateDocument();

        Assert.Equal(ConversionJobStatus.Queued, Assert.Single(service.GetSnapshot().Jobs).Status);
        Assert.Equal(PersistedJobStatus.Queued, Assert.Single(document.Jobs).Status);
        Assert.Equal("libvpx-vp9", document.Jobs[0].Parameters.Values["videoEncoder"]);
    }

    [Fact]
    public void Restore_marks_running_as_interrupted_without_automatically_queueing_or_starting()
    {
        var service = new ConversionQueueService();
        service.Restore(new QueueDocument(QueueDocument.CurrentSchemaVersion,
            [new PersistedConversionJob(Guid.NewGuid(), "input.mp4", "output.mp4", PersistedJobStatus.Running, ConversionParameterSnapshot.CreateDefault())]));

        var restored = Assert.Single(service.GetSnapshot().Jobs);

        Assert.Equal(ConversionJobStatus.Interrupted, restored.Status);
        Assert.Equal(PersistedJobStatus.Interrupted, Assert.Single(service.CreateDocument().Jobs).Status);
    }

    [Fact]
    public void Queue_guards_running_jobs_from_destructive_operations()
    {
        var service = new ConversionQueueService();
        var job = service.Add("input.mp4", "output.mp4", ConversionParameterSnapshot.CreateDefault());
        service.Queue(job.Id);
        service.Start(job.Id);

        Assert.Throws<InvalidOperationException>(() => service.Remove(job.Id));
        Assert.Throws<InvalidOperationException>(service.Clear);
    }
}
