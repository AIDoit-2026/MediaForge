namespace MediaForge.Core.Jobs;

/// <summary>Signals that a conversion completed without writing an output by policy.</summary>
public sealed class ConversionSkippedException : Exception
{
    public ConversionSkippedException(string? message = null)
        : base(message ?? "The conversion was skipped by output conflict policy.") { }
}
