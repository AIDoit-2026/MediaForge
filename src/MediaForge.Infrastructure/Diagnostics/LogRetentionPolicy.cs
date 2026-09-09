namespace MediaForge.Infrastructure.Diagnostics;

public sealed record LogRetentionPolicy(long MaximumActiveFileBytes, int MaximumArchivedFiles)
{
    public static LogRetentionPolicy Default { get; } = new(
        MaximumActiveFileBytes: 2 * 1024 * 1024,
        MaximumArchivedFiles: 7);

    public void Validate()
    {
        if (MaximumActiveFileBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumActiveFileBytes));
        }

        if (MaximumArchivedFiles < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumArchivedFiles));
        }
    }
}
