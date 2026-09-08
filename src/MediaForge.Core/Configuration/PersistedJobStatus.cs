namespace MediaForge.Core.Configuration;

public enum PersistedJobStatus
{
    Ready,
    Queued,
    Running,
    Succeeded,
    Failed,
    Interrupted,
    Skipped
}
