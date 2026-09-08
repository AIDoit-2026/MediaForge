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

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed record TestSettings(string Theme, int Concurrency);
}
