namespace MediaForge.Core.Jobs;

public enum ConversionJobStatus
{
    Probing,
    Ready,
    Invalid,
    Queued,
    Running,
    Succeeded,
    Failed,
    Interrupted,
    Skipped
}
