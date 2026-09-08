using MediaForge.Core.Jobs;

namespace MediaForge.Core.Tests;

public sealed class ConversionJobTests
{
    [Fact]
    public void Restore_marks_a_previously_running_job_as_interrupted()
    {
        var job = new ConversionJob(Guid.NewGuid(), "input.mp4", "output.mkv", ConversionJobStatus.Running);

        job.RestoreAfterApplicationStart();

        Assert.Equal(ConversionJobStatus.Interrupted, job.Status);
    }

    [Fact]
    public void A_ready_job_can_run_and_succeed()
    {
        var job = new ConversionJob(Guid.NewGuid(), "input.mp4", "output.mkv", ConversionJobStatus.Ready);

        job.Queue();
        job.Start();
        job.Complete();

        Assert.Equal(ConversionJobStatus.Succeeded, job.Status);
    }

    [Fact]
    public void An_invalid_transition_is_rejected()
    {
        var job = new ConversionJob(Guid.NewGuid(), "input.mp4", "output.mkv", ConversionJobStatus.Ready);

        var error = Assert.Throws<InvalidOperationException>(() => job.Complete());

        Assert.Contains("Ready", error.Message, StringComparison.Ordinal);
    }
}
