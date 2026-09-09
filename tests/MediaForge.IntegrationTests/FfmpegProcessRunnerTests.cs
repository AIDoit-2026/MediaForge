using MediaForge.Infrastructure.Ffmpeg;

namespace MediaForge.IntegrationTests;

public sealed class FfmpegProcessRunnerTests
{
    [Fact]
    public async Task Run_captures_standard_output_error_and_exit_code_without_a_shell()
    {
        var runner = new FfmpegProcessRunner();

        var result = await runner.RunAsync(
            Path.Combine(Environment.SystemDirectory, "cmd.exe"),
            ["/d", "/c", "echo output & echo error 1>&2 & exit /b 7"]);

        Assert.Equal(7, result.ExitCode);
        Assert.Contains("output", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("error", result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_cancellation_terminates_a_long_running_process()
    {
        var runner = new FfmpegProcessRunner();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.RunAsync(
            Path.Combine(Environment.SystemDirectory, "cmd.exe"),
            ["/d", "/c", "ping -n 10 127.0.0.1 >nul"],
            cancellationSource.Token));
    }
}
