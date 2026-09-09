namespace MediaForge.Core.Jobs;

public interface IConversionQueueScheduler
{
    int MaximumConcurrency { get; }

    void SetMaximumConcurrency(int maximumConcurrency);

    Task StartQueuedJobsAsync();
}
