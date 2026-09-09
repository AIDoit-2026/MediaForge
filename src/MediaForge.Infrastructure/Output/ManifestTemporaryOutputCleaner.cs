using MediaForge.Core.Conversion;

namespace MediaForge.Infrastructure.Output;

/// <summary>
/// Deletes only exact paths explicitly recorded by task manifests. It never searches a directory with a wildcard.
/// </summary>
public sealed class ManifestTemporaryOutputCleaner
{
    public IReadOnlyList<TemporaryOutputCleanupResult> Cleanup(IEnumerable<JobManifest> manifests)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        return manifests.Select(CleanupOne).ToArray();
    }

    private static TemporaryOutputCleanupResult CleanupOne(JobManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!IsExpectedTemporaryOutput(manifest, out var temporaryPath, out var reason))
        {
            return new TemporaryOutputCleanupResult(manifest.JobId, manifest.TemporaryOutputPath, false, reason);
        }

        if (!File.Exists(temporaryPath))
        {
            return new TemporaryOutputCleanupResult(manifest.JobId, temporaryPath, false, "FileNotFound");
        }

        File.Delete(temporaryPath);
        return new TemporaryOutputCleanupResult(manifest.JobId, temporaryPath, true, null);
    }

    private static bool IsExpectedTemporaryOutput(JobManifest manifest, out string temporaryPath, out string reason)
    {
        temporaryPath = string.Empty;
        reason = string.Empty;
        try
        {
            temporaryPath = Path.GetFullPath(manifest.TemporaryOutputPath);
            var outputPath = Path.GetFullPath(manifest.OutputPath);
            var temporaryDirectory = Path.GetDirectoryName(temporaryPath);
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.Equals(temporaryDirectory, outputDirectory, StringComparison.OrdinalIgnoreCase))
            {
                reason = "OutsideOutputDirectory";
                return false;
            }

            if (!Path.GetFileName(temporaryPath).Contains(".mediaforge.tmp", StringComparison.OrdinalIgnoreCase))
            {
                reason = "MissingTemporaryMarker";
                return false;
            }

            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            reason = "InvalidPath";
            return false;
        }
    }
}
