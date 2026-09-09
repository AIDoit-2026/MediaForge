using MediaForge.Core.Configuration;
using MediaForge.Core.Conversion;
using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Naming;

namespace MediaForge.Infrastructure.Output;

/// <summary>
/// Executes a previously built plan without invoking a shell. The final output is only committed after
/// every FFmpeg pass exits successfully and the exact temporary output exists.
/// </summary>
public sealed class FfmpegConversionRunner(
    IFfmpegProcessRunner processRunner,
    OutputFileCommitter outputCommitter) : IConversionRunner
{
    public async Task<ConversionExecutionResult> RunAsync(
        string ffmpegPath,
        FfmpegCommandPlan plan,
        OutputConflictPolicy conflictPolicy,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ffmpegPath);
        ArgumentNullException.ThrowIfNull(plan);

        var manifest = plan.Manifest;
        var initialResolution = OutputConflictResolver.Resolve(manifest.OutputPath, conflictPolicy, File.Exists);
        if (initialResolution.ShouldSkip)
        {
            return new ConversionExecutionResult(false, true, initialResolution.OutputPath, null, string.Empty, string.Empty);
        }

        FfmpegProcessResult? lastResult = null;
        try
        {
            foreach (var invocation in plan.Invocations)
            {
                lastResult = await processRunner.RunAsync(ffmpegPath, invocation.Arguments, cancellationToken);
                if (lastResult.ExitCode != 0)
                {
                    DeleteTemporaryArtifacts(manifest);
                    return new ConversionExecutionResult(
                        Succeeded: false,
                        Skipped: false,
                        OutputPath: null,
                        lastResult.ExitCode,
                        lastResult.StandardOutput,
                        lastResult.StandardError);
                }
            }

            if (!File.Exists(manifest.TemporaryOutputPath))
            {
                DeleteTemporaryArtifacts(manifest);
                return new ConversionExecutionResult(
                    Succeeded: false,
                    Skipped: false,
                    OutputPath: null,
                    ExitCode: lastResult?.ExitCode,
                    StandardOutput: lastResult?.StandardOutput ?? string.Empty,
                    StandardError: "FFmpeg reported success but did not create the expected temporary output.");
            }

            var committed = outputCommitter.Commit(manifest.TemporaryOutputPath, manifest.OutputPath, conflictPolicy);
            DeleteTwoPassLogs(manifest);
            return new ConversionExecutionResult(
                Succeeded: !committed.ShouldSkip,
                Skipped: committed.ShouldSkip,
                OutputPath: committed.ShouldSkip ? null : committed.OutputPath,
                ExitCode: lastResult?.ExitCode,
                StandardOutput: lastResult?.StandardOutput ?? string.Empty,
                StandardError: lastResult?.StandardError ?? string.Empty);
        }
        catch (OperationCanceledException)
        {
            DeleteTemporaryArtifacts(manifest);
            throw;
        }
        catch
        {
            DeleteTemporaryArtifacts(manifest);
            throw;
        }
    }

    private static void DeleteTemporaryArtifacts(JobManifest manifest)
    {
        DeleteIfExists(manifest.TemporaryOutputPath);
        DeleteTwoPassLogs(manifest);
    }

    private static void DeleteTwoPassLogs(JobManifest manifest)
    {
        var prefix = manifest.TemporaryOutputPath + ".passlog";
        DeleteIfExists(prefix + "-0.log");
        DeleteIfExists(prefix + "-0.log.mbtree");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
