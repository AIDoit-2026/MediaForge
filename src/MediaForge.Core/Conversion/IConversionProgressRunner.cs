using MediaForge.Core.Configuration;
using MediaForge.Core.Ffmpeg;

namespace MediaForge.Core.Conversion;

/// <summary>Optional conversion runner capability that exposes FFmpeg progress updates.</summary>
public interface IConversionProgressRunner : IConversionRunner
{
    Task<ConversionExecutionResult> RunWithProgressAsync(
        string ffmpegPath,
        FfmpegCommandPlan plan,
        OutputConflictPolicy conflictPolicy,
        IProgress<FfmpegProgressUpdate>? progress,
        CancellationToken cancellationToken = default);
}
