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
    private readonly Task _writerTask;
    private bool _isDisposed;

    public JsonLineApplicationLogger(IApplicationPaths applicationPaths)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
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
        _writerTask.GetAwaiter().GetResult();
    }

    private async Task WriteEntriesAsync()
    {
        await foreach (var entry in _entries.Reader.ReadAllAsync())
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
                await File.AppendAllTextAsync(
                    _logPath,
                    JsonSerializer.Serialize(entry, SerializerOptions) + Environment.NewLine);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"MediaForge logging failed: {error.Message}");
            }
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
