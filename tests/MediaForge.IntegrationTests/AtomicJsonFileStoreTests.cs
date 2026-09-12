using MediaForge.Core.Configuration;
using MediaForge.Core.Importing;
using MediaForge.Infrastructure.Configuration;

namespace MediaForge.IntegrationTests;

public sealed class AtomicJsonFileStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "MediaForge.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Write_then_read_round_trips_a_configuration_document()
    {
        var path = Path.Combine(_root, "config", "settings.json");
        var store = new AtomicJsonFileStore();

        await store.WriteAsync(path, new TestSettings("dark", 3));
        var result = await store.ReadAsync(path, () => new TestSettings("system", 1));

        Assert.Equal(new TestSettings("dark", 3), result.Value);
        Assert.False(result.RecoveredFromCorruption);
    }

    [Fact]
    public async Task Read_backs_up_invalid_json_and_returns_the_safe_default()
    {
        var path = Path.Combine(_root, "config", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{ not valid json }");
        var store = new AtomicJsonFileStore();

        var result = await store.ReadAsync(path, () => new TestSettings("system", 1));

        Assert.Equal(new TestSettings("system", 1), result.Value);
        Assert.True(result.RecoveredFromCorruption);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!, "settings.json.corrupt-*"));
    }

    [Fact]
    public async Task Settings_store_uses_defaults_without_writing_during_a_temporary_session()
    {
        var paths = new TestApplicationPaths(_root, canPersist: false);
        var store = new ApplicationSettingsStore(paths, new AtomicJsonFileStore());

        var loaded = await store.LoadAsync();
        await store.SaveAsync(ApplicationSettings.CreateDefault() with { FfmpegDirectory = "tools" });

        Assert.Equal(ApplicationSettings.CreateDefault(), loaded.Settings);
        Assert.False(Directory.Exists(paths.ConfigDirectory));
    }

    [Fact]
    public async Task Settings_store_persists_the_user_editable_settings()
    {
        var paths = new TestApplicationPaths(_root, canPersist: true);
        var store = new ApplicationSettingsStore(paths, new AtomicJsonFileStore());
        var expected = ApplicationSettings.CreateDefault() with
        {
            Language = ApplicationLanguage.English,
            Theme = ApplicationTheme.Dark,
            FfmpegDirectory = "C:\\Tools\\ffmpeg",
            DefaultOutputDirectory = "D:\\Converted",
            OutputConflictPolicy = OutputConflictPolicy.Skip,
            MaxConcurrentJobs = 3,
            WindowWidth = 1280,
            WindowHeight = 720,
            LastFolderImport = new FolderImportSettings(
                "D:\\Media",
                IncludeSubdirectories: false,
                FilterMode: FileNameFilterMode.Wildcard,
                FilterExpression: "*.mp4")
        };

        await store.SaveAsync(expected);
        var loaded = await store.LoadAsync();

        Assert.Equal(expected, loaded.Settings);
        Assert.False(loaded.RecoveredFromCorruption);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed record TestSettings(string Theme, int Concurrency);

    private sealed class TestApplicationPaths : IApplicationPaths
    {
        public TestApplicationPaths(string baseDirectory, bool canPersist)
        {
            BaseDirectory = baseDirectory;
            ConfigDirectory = Path.Combine(baseDirectory, "config");
            CanPersist = canPersist;
        }

        public string BaseDirectory { get; }

        public string ConfigDirectory { get; }

        public bool CanPersist { get; }

        public string? PersistenceWarningCode => null;
    }
}
