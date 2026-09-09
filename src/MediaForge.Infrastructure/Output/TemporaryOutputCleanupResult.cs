namespace MediaForge.Infrastructure.Output;

public sealed record TemporaryOutputCleanupResult(Guid JobId, string TemporaryPath, bool Deleted, string? SkipReason);
