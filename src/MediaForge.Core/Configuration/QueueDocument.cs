namespace MediaForge.Core.Configuration;

public sealed record QueueDocument(int SchemaVersion, IReadOnlyList<PersistedConversionJob> Jobs)
{
    public const int CurrentSchemaVersion = 1;

    public static QueueDocument CreateEmpty() => new(CurrentSchemaVersion, []);

    public QueueDocument RestoreAfterApplicationStart() => this with
    {
        Jobs = Jobs
            .Select(job => job.Status == PersistedJobStatus.Running
                ? job with { Status = PersistedJobStatus.Interrupted }
                : job)
            .ToArray()
    };
}
