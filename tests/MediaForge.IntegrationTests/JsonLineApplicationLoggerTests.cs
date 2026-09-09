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

    [Fact]
    public void Rotation_keeps_the_active_log_and_only_the_newest_archives()
    {
        var paths = new TestApplicationPaths(_root);
        var logDirectory = Path.Combine(paths.ConfigDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        var activeLog = Path.Combine(logDirectory, "application.ndjson");
        File.WriteAllText(activeLog, new string('x', 40));
        File.WriteAllText(Path.Combine(logDirectory, "application.20000101000000000.old.ndjson"), "old");

        using (var logger = new JsonLineApplicationLogger(paths, new LogRetentionPolicy(32, 1)))
        {
            logger.Log(new ApplicationLogEntry(DateTimeOffset.UtcNow, ApplicationLogLevel.Information, "Rotated"));
        }

        Assert.True(File.Exists(activeLog));
        Assert.Contains("Rotated", File.ReadAllText(activeLog), StringComparison.Ordinal);
        var archives = Directory.GetFiles(logDirectory, "application.*.ndjson");
        Assert.Single(archives);
        Assert.Contains(new string('x', 40), File.ReadAllText(archives[0]), StringComparison.Ordinal);
    }

    [Fact]
    public void Writing_prunes_excess_archives_even_before_the_active_log_needs_rotation()
    {
        var paths = new TestApplicationPaths(_root);
        var logDirectory = Path.Combine(paths.ConfigDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        File.WriteAllText(Path.Combine(logDirectory, "application.ndjson"), "small");
        File.WriteAllText(Path.Combine(logDirectory, "application.20000101000000000.old.ndjson"), "old");
        File.WriteAllText(Path.Combine(logDirectory, "application.20200101000000000.new.ndjson"), "new");

        using (var logger = new JsonLineApplicationLogger(paths, new LogRetentionPolicy(1024, 1)))
        {
            logger.Log(new ApplicationLogEntry(DateTimeOffset.UtcNow, ApplicationLogLevel.Information, "Pruned"));
        }

        var archive = Assert.Single(Directory.GetFiles(logDirectory, "application.*.ndjson"));
        Assert.EndsWith("application.20200101000000000.new.ndjson", archive, StringComparison.Ordinal);
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
