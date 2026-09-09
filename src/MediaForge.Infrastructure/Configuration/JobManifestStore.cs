using MediaForge.Core.Configuration;
using MediaForge.Core.Conversion;

namespace MediaForge.Infrastructure.Configuration;

public sealed class JobManifestStore(IApplicationPaths paths, AtomicJsonFileStore store) : IJobManifestStore
{
    public async Task<IReadOnlyList<JobManifest>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!paths.CanPersist) return [];
        var directory = GetDirectory();
        if (!Directory.Exists(directory)) return [];

        var manifests = new List<JobManifest>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            var result = await store.ReadAsync<JobManifest?>(path, () => null, cancellationToken);
            if (result.Value is not null) manifests.Add(result.Value);
        }
        return manifests;
    }

    public Task SaveAsync(JobManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentOutOfRangeException.ThrowIfEqual(manifest.JobId, Guid.Empty);
        return paths.CanPersist
            ? store.WriteAsync(Path.Combine(GetDirectory(), $"{manifest.JobId:N}.json"), manifest, cancellationToken)
            : Task.CompletedTask;
    }

    public Task DeleteAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(jobId, Guid.Empty);
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(GetDirectory(), $"{jobId:N}.json");
        if (paths.CanPersist && File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string GetDirectory() => Path.Combine(paths.ConfigDirectory, "manifests");
}
