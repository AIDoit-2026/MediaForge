using MediaForge.Core.Configuration;
using MediaForge.Infrastructure.Configuration;

namespace MediaForge.IntegrationTests;

public sealed class UserPresetStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "MediaForge.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Create_save_as_rename_and_delete_manage_independent_preset_files()
    {
        var paths = new Paths(_root, canPersist: true);
        var store = new UserPresetStore(paths, new AtomicJsonFileStore());
        var first = store.Create("  Archive  ", ConversionParameterSnapshot.CreateDefault());

        await store.SaveAsync(first);
        var copy = await store.SaveAsAsync(first, "Mobile");
        var renamed = await store.RenameAsync(first, "Archive lossless");
        var loaded = await store.LoadAsync();

        Assert.Equal("Archive lossless", renamed.Name);
        Assert.Equal("Archive", first.Name);
        Assert.NotEqual(first.Id, copy.Id);
        Assert.Equal([renamed.Id, copy.Id], loaded.Select(preset => preset.Id));
        Assert.Equal([renamed.Name, copy.Name], loaded.Select(preset => preset.Name));
        Assert.Equal(2, Directory.GetFiles(Path.Combine(paths.ConfigDirectory, "presets"), "*.json").Length);

        await store.DeleteAsync(copy.Id);

        var remaining = Assert.Single(await store.LoadAsync());
        Assert.Equal(renamed.Id, remaining.Id);
        Assert.Equal(renamed.Name, remaining.Name);
    }

    [Fact]
    public async Task Load_migrates_a_legacy_schema_zero_document()
    {
        var paths = new Paths(_root, canPersist: true);
        var json = new AtomicJsonFileStore();
        var legacy = new PresetDocument(0, Guid.NewGuid(), "Legacy", ConversionParameterSnapshot.CreateDefault());
        await json.WriteAsync(Path.Combine(paths.ConfigDirectory, "presets", $"{legacy.Id:N}.json"), legacy);

        var loaded = await new UserPresetStore(paths, json).LoadAsync();

        Assert.Equal(PresetDocument.CurrentSchemaVersion, Assert.Single(loaded).SchemaVersion);
    }

    [Fact]
    public async Task Temporary_session_keeps_user_presets_in_memory_only()
    {
        var paths = new Paths(_root, canPersist: false);
        var store = new UserPresetStore(paths, new AtomicJsonFileStore());
        var preset = store.Create("Draft", ConversionParameterSnapshot.CreateDefault());

        await store.SaveAsync(preset);

        Assert.Empty(await store.LoadAsync());
        Assert.False(Directory.Exists(paths.ConfigDirectory));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private sealed class Paths(string root, bool canPersist) : IApplicationPaths
    {
        public string BaseDirectory => root;
        public string ConfigDirectory => Path.Combine(root, "config");
        public bool CanPersist => canPersist;
        public string? PersistenceWarningCode => null;
    }
}
