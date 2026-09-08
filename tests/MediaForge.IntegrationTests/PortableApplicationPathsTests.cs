using MediaForge.Infrastructure.Configuration;

namespace MediaForge.IntegrationTests;

public sealed class PortableApplicationPathsTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "MediaForge.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Create_uses_a_writable_config_directory_beside_the_executable()
    {
        Directory.CreateDirectory(_root);

        var paths = PortableApplicationPaths.Create(_root);

        Assert.True(paths.CanPersist);
        Assert.Equal(Path.Combine(_root, "config"), paths.ConfigDirectory);
        Assert.Null(paths.PersistenceWarningCode);
        Assert.True(Directory.Exists(paths.ConfigDirectory));
    }

    [Fact]
    public void Create_falls_back_to_a_temporary_session_when_config_is_not_a_directory()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "config"), "occupied");

        var paths = PortableApplicationPaths.Create(_root);

        Assert.False(paths.CanPersist);
        Assert.Equal("ConfigurationDirectoryUnavailable", paths.PersistenceWarningCode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
