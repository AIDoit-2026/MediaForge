namespace MediaForge.Core.Diagnostics;

public interface IApplicationLogger
{
    void Log(ApplicationLogEntry entry);

    void LogError(string eventName, Exception exception, string? message = null);
}
