namespace MediaForge.Core.Diagnostics;

public sealed record ApplicationLogEntry(
    DateTimeOffset Timestamp,
    ApplicationLogLevel Level,
    string EventName,
    string? Message = null,
    string? ExceptionType = null,
    string? ExceptionMessage = null);
