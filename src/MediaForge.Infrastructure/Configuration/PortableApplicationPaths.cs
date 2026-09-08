using System.Security;
using MediaForge.Core.Configuration;

namespace MediaForge.Infrastructure.Configuration;

public sealed class PortableApplicationPaths : IApplicationPaths
{
    public const string ConfigurationDirectoryUnavailable =
        nameof(ConfigurationDirectoryUnavailable);

    private PortableApplicationPaths(
        string baseDirectory,
        string configDirectory,
        bool canPersist,
        string? persistenceWarningCode)
    {
        BaseDirectory = baseDirectory;
        ConfigDirectory = configDirectory;
        CanPersist = canPersist;
        PersistenceWarningCode = persistenceWarningCode;
    }

    public string BaseDirectory { get; }

    public string ConfigDirectory { get; }

    public bool CanPersist { get; }

    public string? PersistenceWarningCode { get; }

    public static PortableApplicationPaths Create(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var normalizedBaseDirectory = Path.GetFullPath(baseDirectory);
        var configDirectory = Path.Combine(normalizedBaseDirectory, "config");

        try
        {
            Directory.CreateDirectory(configDirectory);
            ProbeWriteAccess(configDirectory);

            return new PortableApplicationPaths(
                normalizedBaseDirectory,
                configDirectory,
                canPersist: true,
                persistenceWarningCode: null);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            return new PortableApplicationPaths(
                normalizedBaseDirectory,
                configDirectory,
                canPersist: false,
                persistenceWarningCode: ConfigurationDirectoryUnavailable);
        }
    }

    private static void ProbeWriteAccess(string directory)
    {
        var probePath = Path.Combine(directory, $".write-probe-{Guid.NewGuid():N}.tmp");
        using var probe = new FileStream(
            probePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1,
            FileOptions.DeleteOnClose);

        probe.WriteByte(0);
        probe.Flush(flushToDisk: true);
    }
}
