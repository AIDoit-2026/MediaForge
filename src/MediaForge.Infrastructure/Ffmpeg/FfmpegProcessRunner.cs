using System.Diagnostics;
using MediaForge.Core.Ffmpeg;

namespace MediaForge.Infrastructure.Ffmpeg;

public sealed class FfmpegProcessRunner : IStreamingFfmpegProcessRunner
{
    public async Task<FfmpegProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);

        return await RunWithOutputObserverAsync(executablePath, arguments, null, cancellationToken);
    }

    public async Task<FfmpegProcessResult> RunWithOutputObserverAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        Action<string>? standardOutputLine,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(executablePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var standardOutputTask = ReadStandardOutputAsync(process.StandardOutput, standardOutputLine);
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            StopProcessTree(process);
            await Task.WhenAll(standardOutputTask, standardErrorTask);
            throw;
        }

        return new FfmpegProcessResult(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask);
    }

    private static async Task<string> ReadStandardOutputAsync(StreamReader reader, Action<string>? lineObserver)
    {
        var output = new System.Text.StringBuilder();
        while (await reader.ReadLineAsync() is { } line)
        {
            output.AppendLine(line);
            lineObserver?.Invoke(line);
        }
        return output.ToString();
    }

    private static void StopProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The child exited after the status check.
        }
    }
}
