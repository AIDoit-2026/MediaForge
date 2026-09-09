using MediaForge.Core.Configuration;
using MediaForge.Infrastructure.Output;

namespace MediaForge.App.Services;

public sealed class TemporaryOutputRecoveryService(
    IJobManifestStore manifestStore,
    ManifestTemporaryOutputCleaner cleaner)
{
    public async Task<IReadOnlyList<TemporaryOutputCleanupResult>> RecoverAsync(CancellationToken cancellationToken = default)
    {
        var manifests = await manifestStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var results = cleaner.Cleanup(manifests);
        foreach (var result in results.Where(result => result.Deleted || result.SkipReason == "FileNotFound"))
        {
            await manifestStore.DeleteAsync(result.JobId, cancellationToken).ConfigureAwait(false);
        }
        return results;
    }
}
