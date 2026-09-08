using MediaForge.Core.Configuration;
using MediaForge.Core.Diagnostics;
using MediaForge.Infrastructure.Diagnostics;

namespace MediaForge.IntegrationTests;

public sealed class JsonLineApplicationLoggerTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "MediaForge.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Dispose_flushes_structured_entries_to_the_portable_log_file()
    {
        var paths = new TestApplicationPaths(_root);
        using (var logger = new JsonLineApplicationLogger(paths))
        {
            logger.Log(new ApplicationLogEntry(
                DateTimeOffset.UtcNow,
                ApplicationLogLevel.Information,
                "ApplicationLaunched"));
        }

        var logPath = Path.Combine(paths.ConfigDirectory, "logs", "application.ndjson");
        var log = File.ReadAllText(logPath);

        Assert.Contains("ApplicationLaunched", log, StringComparison.Ordinal);
        Assert.Contains("information", log, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class TestApplicationPaths : IApplicationPaths
    {
        public TestApplicationPaths(string baseDirectory)
        {
            BaseDirectory = baseDirectory;
            ConfigDirectory = Path.Combine(baseDirectory, "config");
        }

        public string BaseDirectory { get; }

        public string ConfigDirectory { get; }

        public bool CanPersist => true;

        public string? PersistenceWarningCode => null;
    }
}
