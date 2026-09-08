using System.Text.Json;

namespace MediaForge.Infrastructure.Configuration;

public sealed class AtomicJsonFileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(value);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The configuration path has no parent directory.");

        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, value, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public async Task<ConfigurationReadResult<T>> ReadAsync<T>(
        string path,
        Func<T> defaultFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(defaultFactory);

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            return new ConfigurationReadResult<T>(defaultFactory(), RecoveredFromCorruption: false);
        }

        try
        {
            T? value;
            await using (var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                value = await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
            }

            return new ConfigurationReadResult<T>(
                value ?? throw new JsonException("The configuration document cannot be null."),
                RecoveredFromCorruption: false);
        }
        catch (JsonException)
        {
            BackupCorruptFile(fullPath);
            return new ConfigurationReadResult<T>(defaultFactory(), RecoveredFromCorruption: true);
        }
    }

    private static void BackupCorruptFile(string path)
    {
        var backupPath = $"{path}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        File.Move(path, backupPath);
    }
}
