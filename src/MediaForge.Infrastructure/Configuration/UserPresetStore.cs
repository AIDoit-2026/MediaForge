using MediaForge.Core.Configuration;

namespace MediaForge.Infrastructure.Configuration;

public sealed class UserPresetStore(IApplicationPaths paths, AtomicJsonFileStore store) : IUserPresetStore
{
    public async Task<IReadOnlyList<PresetDocument>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!paths.CanPersist) return [];
        var directory = Path.Combine(paths.ConfigDirectory, "presets");
        if (!Directory.Exists(directory)) return [];
        var presets = new List<PresetDocument>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            var result = await store.ReadAsync<PresetDocument?>(file, () => null, cancellationToken);
            if (result.Value is not null) presets.Add(PresetDocumentMigration.MigrateToCurrent(result.Value));
        }
        return presets.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public PresetDocument Create(string name, ConversionParameterSnapshot parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return new PresetDocument(PresetDocument.CurrentSchemaVersion, Guid.NewGuid(), NormalizeName(name), parameters);
    }

    public Task SaveAsync(PresetDocument preset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (preset.Id == Guid.Empty)
        {
            throw new ArgumentException("A preset must have an identifier.", nameof(preset));
        }

        var currentPreset = PresetDocumentMigration.MigrateToCurrent(preset) with { Name = NormalizeName(preset.Name) };
        return paths.CanPersist
            ? store.WriteAsync(Path.Combine(paths.ConfigDirectory, "presets", $"{currentPreset.Id:N}.json"), currentPreset, cancellationToken)
            : Task.CompletedTask;
    }

    public async Task<PresetDocument> SaveAsAsync(PresetDocument source, string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var copy = Create(name, source.Parameters);
        await SaveAsync(copy, cancellationToken);
        return copy;
    }

    public async Task<PresetDocument> RenameAsync(PresetDocument preset, string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preset);
        var renamed = PresetDocumentMigration.MigrateToCurrent(preset) with { Name = NormalizeName(name) };
        await SaveAsync(renamed, cancellationToken);
        return renamed;
    }

    public Task DeleteAsync(Guid presetId, CancellationToken cancellationToken = default)
    {
        if (presetId == Guid.Empty)
        {
            throw new ArgumentException("A preset identifier is required.", nameof(presetId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(paths.ConfigDirectory, "presets", $"{presetId:N}.json");
        if (paths.CanPersist && File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
