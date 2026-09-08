using MediaForge.Core.Configuration;

namespace MediaForge.Infrastructure.Configuration;

public sealed class ApplicationSettingsStore : IApplicationSettingsStore
{
    private const string SettingsFileName = "settings.json";

    private readonly IApplicationPaths _applicationPaths;
    private readonly AtomicJsonFileStore _jsonStore;

    public ApplicationSettingsStore(IApplicationPaths applicationPaths, AtomicJsonFileStore jsonStore)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
        ArgumentNullException.ThrowIfNull(jsonStore);

        _applicationPaths = applicationPaths;
        _jsonStore = jsonStore;
    }

    public async Task<SettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_applicationPaths.CanPersist)
        {
            return new SettingsLoadResult(ApplicationSettings.CreateDefault(), RecoveredFromCorruption: false);
        }

        var result = await _jsonStore.ReadAsync(
            GetSettingsPath(),
            ApplicationSettings.CreateDefault,
            cancellationToken);

        return new SettingsLoadResult(result.Value, result.RecoveredFromCorruption);
    }

    public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return _applicationPaths.CanPersist
            ? _jsonStore.WriteAsync(GetSettingsPath(), settings, cancellationToken)
            : Task.CompletedTask;
    }

    private string GetSettingsPath() => Path.Combine(_applicationPaths.ConfigDirectory, SettingsFileName);
}
