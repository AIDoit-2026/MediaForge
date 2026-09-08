using System.Diagnostics;
using MediaForge.Core.Ffmpeg;

namespace MediaForge.Infrastructure.Ffmpeg;

public sealed class FfmpegVersionReader : IFfmpegVersionReader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<FfmpegVersionReadResult> ReadAsync(
        FfmpegToolset toolset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolset);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(toolset.FfmpegPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.StartInfo.ArgumentList.Add("-version");

        try
        {
            process.Start();
            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(Timeout);

            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryStop(process);
                await Task.WhenAll(standardOutputTask, standardErrorTask);
                return FfmpegVersionReadResult.Failure(FfmpegVersionReadResult.VersionCommandTimedOut);
            }

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;
            if (process.ExitCode != 0)
            {
                return FfmpegVersionReadResult.Failure(
                    FfmpegVersionReadResult.VersionCommandFailed,
                    string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError);
            }

            return FfmpegVersionParser.Parse(standardOutput);
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return FfmpegVersionReadResult.Failure(FfmpegVersionReadResult.VersionCommandFailed, error.Message);
        }
    }

    private static void TryStop(Process process)
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
            // The process exited between the status check and the stop request.
        }
    }
}
