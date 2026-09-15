using MediaForge.Core.Configuration;

namespace MediaForge.Infrastructure.Configuration;

public sealed class UserPresetStore(IApplicationPaths paths, AtomicJsonFileStore store) : IUserPresetStore
{
    public async Task<IReadOnlyList<PresetDocument>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(paths.BaseDirectory, "Presents");
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

    public async Task SaveAsync(PresetDocument preset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (preset.Id == Guid.Empty)
        {
            throw new ArgumentException("A preset must have an identifier.", nameof(preset));
        }

        var currentPreset = PresetDocumentMigration.MigrateToCurrent(preset) with { Name = NormalizeName(preset.Name) };
        if (!paths.CanPersist) return;

        var directory = Path.Combine(paths.BaseDirectory, "Presents");
        Directory.CreateDirectory(directory);
        var targetPath = Path.Combine(directory, $"{FileNameFor(currentPreset)}.json");
        await store.WriteAsync(targetPath, currentPreset, cancellationToken);
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(path, targetPath, StringComparison.OrdinalIgnoreCase)) continue;
            var result = await store.ReadAsync<PresetDocument?>(path, () => null, cancellationToken);
            if (result.Value?.Id == currentPreset.Id) File.Delete(path);
        }
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

    public async Task DeleteAsync(Guid presetId, CancellationToken cancellationToken = default)
    {
        if (presetId == Guid.Empty)
        {
            throw new ArgumentException("A preset identifier is required.", nameof(presetId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!paths.CanPersist) return;
        var directory = Path.Combine(paths.BaseDirectory, "Presents");
        if (!Directory.Exists(directory)) return;
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await store.ReadAsync<PresetDocument?>(path, () => null, cancellationToken);
            if (result.Value?.Id == presetId)
            {
                File.Delete(path);
            }
        }
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }

    private static string FileNameFor(PresetDocument preset)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new System.Text.StringBuilder(preset.Name.Length);
        foreach (var character in preset.Name.Trim())
        {
            if (invalid.Contains(character) || char.IsControl(character) || char.IsWhiteSpace(character)) builder.Append('-');
            else builder.Append(character);
        }

        var slug = builder.ToString().Trim('.', ' ', '-');
        if (slug.Length == 0) slug = "preset";
        if (slug.Length > 80) slug = slug[..80].TrimEnd('-', '.');
        return $"{slug}-{preset.Id:N}";
    }
}
