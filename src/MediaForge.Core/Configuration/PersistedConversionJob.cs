namespace MediaForge.Core.Configuration;

public sealed record PersistedConversionJob(
    Guid Id,
    string InputPath,
    string OutputPath,
    PersistedJobStatus Status,
    ConversionParameterSnapshot Parameters);
