using MediaForge.Core.Ffmpeg;
using MediaForge.Infrastructure.Ffmpeg;

namespace MediaForge.IntegrationTests;

public sealed class FfmpegToolResolverTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "MediaForge.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Resolve_prefers_a_complete_user_configured_directory()
    {
        var appDirectory = CreateToolDirectory("app");
        var configuredDirectory = CreateToolDirectory("configured");
        var resolver = new FfmpegToolResolver(appDirectory);

        var result = resolver.Resolve(configuredDirectory);

        Assert.True(result.IsSuccess);
        Assert.Equal(configuredDirectory, result.Toolset!.Directory);
        Assert.Equal(FfmpegToolSource.ConfiguredDirectory, result.Toolset.Source);
    }

    [Fact]
    public void Resolve_falls_back_to_the_application_directory_when_configured_tools_are_incomplete()
    {
        var appDirectory = CreateToolDirectory("app");
        var configuredDirectory = Path.Combine(_root, "incomplete");
        Directory.CreateDirectory(configuredDirectory);
        File.WriteAllBytes(Path.Combine(configuredDirectory, "ffmpeg.exe"), []);
        var resolver = new FfmpegToolResolver(appDirectory);

        var result = resolver.Resolve(configuredDirectory);

        Assert.True(result.IsSuccess);
        Assert.Equal(appDirectory, result.Toolset!.Directory);
        Assert.Equal(FfmpegToolSource.ApplicationDirectory, result.Toolset.Source);
    }

    [Fact]
    public async Task Version_reader_returns_a_structured_error_when_the_tool_cannot_start()
    {
        var toolset = new FfmpegToolset(
            _root,
            Path.Combine(_root, "missing-ffmpeg.exe"),
            Path.Combine(_root, "missing-ffprobe.exe"),
            FfmpegToolSource.ConfiguredDirectory);

        var result = await new FfmpegVersionReader().ReadAsync(toolset);

        Assert.False(result.IsSuccess);
        Assert.Equal(FfmpegVersionReadResult.VersionCommandFailed, result.ErrorCode);
    }

    private string CreateToolDirectory(string name)
    {
        var directory = Path.Combine(_root, name);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "ffmpeg.exe"), []);
        File.WriteAllBytes(Path.Combine(directory, "ffprobe.exe"), []);
        return directory;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
