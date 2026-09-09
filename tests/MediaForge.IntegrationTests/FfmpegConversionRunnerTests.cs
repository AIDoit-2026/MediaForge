using MediaForge.Core.Configuration;
using MediaForge.Core.Conversion;
using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Media;
using MediaForge.Infrastructure.Output;

namespace MediaForge.IntegrationTests;

public sealed class FfmpegConversionRunnerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "MediaForge.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Successful_execution_commits_the_temporary_file_and_rechecks_auto_rename()
    {
        Directory.CreateDirectory(_root);
        var output = Path.Combine(_root, "out.mp4");
        File.WriteAllText(output, "existing");
        var temporary = TemporaryOutputPathFactory.Create(output, Guid.NewGuid());
        var plan = Plan(output, temporary);
        var runner = new FfmpegConversionRunner(new WritingProcessRunner(), new OutputFileCommitter());

        var result = await runner.RunAsync("ffmpeg.exe", plan, OutputConflictPolicy.AutoRename);

        Assert.True(result.Succeeded);
        Assert.EndsWith("out (1).mp4", result.OutputPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("converted", File.ReadAllText(result.OutputPath!));
        Assert.False(File.Exists(temporary));
    }

    [Fact]
    public async Task Failed_execution_deletes_only_its_known_temporary_artifacts()
    {
        Directory.CreateDirectory(_root);
        var output = Path.Combine(_root, "out.mp4");
        var temporary = TemporaryOutputPathFactory.Create(output, Guid.NewGuid());
        File.WriteAllText(temporary, "partial");
        var unrelated = Path.Combine(_root, ".other.mediaforge.tmp.mp4");
        File.WriteAllText(unrelated, "keep");
        var plan = Plan(output, temporary);
        var runner = new FfmpegConversionRunner(new FailingProcessRunner(), new OutputFileCommitter());

        var result = await runner.RunAsync("ffmpeg.exe", plan, OutputConflictPolicy.AutoRename);

        Assert.False(result.Succeeded);
        Assert.Equal(12, result.ExitCode);
        Assert.False(File.Exists(temporary));
        Assert.True(File.Exists(unrelated));
    }

    [Fact]
    public async Task Existing_output_is_skipped_before_starting_ffmpeg_when_policy_is_skip()
    {
        Directory.CreateDirectory(_root);
        var output = Path.Combine(_root, "out.mp4");
        File.WriteAllText(output, "existing");
        var process = new WritingProcessRunner();
        var runner = new FfmpegConversionRunner(process, new OutputFileCommitter());

        var result = await runner.RunAsync("ffmpeg.exe", Plan(output, TemporaryOutputPathFactory.Create(output, Guid.NewGuid())), OutputConflictPolicy.Skip);

        Assert.True(result.Skipped);
        Assert.Equal(0, process.CallCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static FfmpegCommandPlan Plan(string output, string temporary)
    {
        var job = new ConversionJobSpec(
            new MediaSourceInfo("in.mp4", "mp4", null, null, null, []),
            output,
            temporary,
            ConversionProfile.CreateDefault(),
            DateTimeOffset.UtcNow);
        return new FfmpegCommandBuilder().Build(job);
    }

    private sealed class WritingProcessRunner : IFfmpegProcessRunner
    {
        public int CallCount { get; private set; }

        public Task<FfmpegProcessResult> RunAsync(string executablePath, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
        {
            CallCount++;
            File.WriteAllText(arguments[^1], "converted");
            return Task.FromResult(new FfmpegProcessResult(0, "ok", string.Empty));
        }
    }

    private sealed class FailingProcessRunner : IFfmpegProcessRunner
    {
        public Task<FfmpegProcessResult> RunAsync(string executablePath, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FfmpegProcessResult(12, string.Empty, "failed"));
    }
}
