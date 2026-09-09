using MediaForge.Core.Conversion;

namespace MediaForge.Core.Configuration;

public interface IJobManifestStore
{
    Task<IReadOnlyList<JobManifest>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(JobManifest manifest, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid jobId, CancellationToken cancellationToken = default);
}
