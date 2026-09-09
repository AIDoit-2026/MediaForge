namespace MediaForge.Core.Conversion;

public sealed record ConversionExecutionResult(
    bool Succeeded,
    bool Skipped,
    string? OutputPath,
    int? ExitCode,
    string StandardOutput,
    string StandardError);
