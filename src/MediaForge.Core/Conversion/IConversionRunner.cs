using MediaForge.Core.Configuration;

namespace MediaForge.Core.Conversion;

public interface IConversionRunner
{
    Task<ConversionExecutionResult> RunAsync(
        string ffmpegPath,
        FfmpegCommandPlan plan,
        OutputConflictPolicy conflictPolicy,
        CancellationToken cancellationToken = default);
}
