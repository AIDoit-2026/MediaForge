using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using MediaForge.Core.Configuration;
using MediaForge.Core.Diagnostics;

namespace MediaForge.Infrastructure.Diagnostics;

public sealed class JsonLineApplicationLogger : IApplicationLogger, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    private readonly Channel<ApplicationLogEntry> _entries = Channel.CreateUnbounded<ApplicationLogEntry>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly string? _logPath;
    private readonly LogRetentionPolicy _retentionPolicy;
    private readonly Task _writerTask;
    private bool _isDisposed;

    public JsonLineApplicationLogger(IApplicationPaths applicationPaths, LogRetentionPolicy? retentionPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
        _retentionPolicy = retentionPolicy ?? LogRetentionPolicy.Default;
        _retentionPolicy.Validate();
        _logPath = applicationPaths.CanPersist
            ? Path.Combine(applicationPaths.ConfigDirectory, "logs", "application.ndjson")
            : null;
        _writerTask = WriteEntriesAsync();
    }

    public void Log(ApplicationLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!_isDisposed)
        {
            _entries.Writer.TryWrite(entry);
        }
    }

    public void LogError(string eventName, Exception exception, string? message = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(exception);

        Log(new ApplicationLogEntry(
            DateTimeOffset.UtcNow,
            ApplicationLogLevel.Error,
            eventName,
            message,
            exception.GetType().FullName,
            exception.Message));
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _entries.Writer.TryComplete();
        // Disposal is called from the UI shutdown path; never wait indefinitely on file I/O.
        _writerTask.Wait(TimeSpan.FromSeconds(1));
    }

    private async Task WriteEntriesAsync()
    {
        await foreach (var entry in _entries.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            if (_logPath is null)
            {
                Debug.WriteLine(JsonSerializer.Serialize(entry, SerializerOptions));
                continue;
            }

            try
            {
                var directory = Path.GetDirectoryName(_logPath)!;
                Directory.CreateDirectory(directory);
                RotateIfRequired();
                await File.AppendAllTextAsync(
                    _logPath,
                    JsonSerializer.Serialize(entry, SerializerOptions) + Environment.NewLine)
                    .ConfigureAwait(false);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"MediaForge logging failed: {error.Message}");
            }
        }
    }

    private void RotateIfRequired()
    {
        var directory = Path.GetDirectoryName(_logPath)!;
        if (File.Exists(_logPath) && new FileInfo(_logPath).Length >= _retentionPolicy.MaximumActiveFileBytes)
        {
            var archivePath = Path.Combine(
                directory,
                $"application.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.{Guid.NewGuid():N}.ndjson");
            File.Move(_logPath, archivePath);
        }

        var archives = Directory.EnumerateFiles(directory, "application.*.ndjson")
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
        foreach (var archive in archives.Skip(_retentionPolicy.MaximumArchivedFiles))
        {
            File.Delete(archive);
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
