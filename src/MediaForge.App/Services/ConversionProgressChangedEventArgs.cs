namespace MediaForge.App.Services;

public sealed record ConversionProgressSnapshot(
    Guid JobId,
    double? Percentage,
    TimeSpan? ProcessedDuration,
    double? Speed,
    TimeSpan Elapsed,
    TimeSpan? EstimatedRemaining,
    DateTimeOffset UpdatedAt);

public sealed class ConversionProgressChangedEventArgs(ConversionProgressSnapshot progress) : EventArgs
{
    public ConversionProgressSnapshot Progress { get; } = progress;
}
