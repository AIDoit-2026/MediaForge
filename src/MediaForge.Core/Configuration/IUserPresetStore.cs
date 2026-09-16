namespace MediaForge.Core.Configuration;

public interface IUserPresetStore
{
    Task<IReadOnlyList<PresetDocument>> LoadAsync(CancellationToken cancellationToken = default);
    Task EnsureBuiltInPresetsAsync(CancellationToken cancellationToken = default);
    PresetDocument Create(string name, ConversionParameterSnapshot parameters);
    Task SaveAsync(PresetDocument preset, CancellationToken cancellationToken = default);
    Task<PresetDocument> SaveAsAsync(PresetDocument source, string name, CancellationToken cancellationToken = default);
    Task<PresetDocument> RenameAsync(PresetDocument preset, string name, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid presetId, CancellationToken cancellationToken = default);
}
