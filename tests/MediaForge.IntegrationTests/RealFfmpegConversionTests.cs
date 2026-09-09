using MediaForge.Core.Configuration;
using MediaForge.Core.Conversion;
using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Media;
using MediaForge.Infrastructure.Ffmpeg;
using MediaForge.Infrastructure.Output;
using Xunit.Sdk;

namespace MediaForge.IntegrationTests;

public sealed class RealFfmpegConversionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "MediaForge.RealFfmpegTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Generated_sample_exercises_video_to_video_video_to_audio_and_audio_to_audio()
    {
        var toolDirectory = FindToolDirectory() ?? throw SkipException.ForSkip("A local ffmpeg/ffprobe toolset was not found.");
        Directory.CreateDirectory(_root);
        var ffmpegPath = Path.Combine(toolDirectory, "ffmpeg.exe");
        var ffprobePath = Path.Combine(toolDirectory, "ffprobe.exe");
        var process = new FfmpegProcessRunner();
        var toolset = new FfmpegToolset(toolDirectory, ffmpegPath, ffprobePath, FfmpegToolSource.ApplicationDirectory);
        var probe = new FfprobeMediaProbeService(toolset, process);
        var inputPath = Path.Combine(_root, "generated-input.mp4");

        var generated = await process.RunAsync(ffmpegPath,
        [
            "-hide_banner", "-y", "-f", "lavfi", "-i", "testsrc2=size=320x180:rate=25",
            "-f", "lavfi", "-i", "sine=frequency=1000:sample_rate=48000", "-t", "1",
            "-metadata", "artist=MediaForge", "-metadata", "title=Generated sample", "-metadata", "album=Test collection",
            "-c:v", "libx264", "-pix_fmt", "yuv420p", "-c:a", "aac", inputPath
        ]);
        Assert.Equal(0, generated.ExitCode);
        var input = await probe.ProbeAsync(inputPath);

        var videoOutput = await ConvertAsync(input, "video-output.mp4", ConversionProfile.CreateDefault() with
        {
            Filters = new VideoFilterSettings(null, new FrameSize(160, 90), VideoRotation.None, null)
        }, ffmpegPath, probe, process);
        var videoInfo = await probe.ProbeAsync(videoOutput);
        Assert.Contains(videoInfo.Streams, stream => stream.Type == MediaStreamType.Video && stream.Width == 160 && stream.Height == 90);
        Assert.Contains(videoInfo.Streams, stream => stream.Type == MediaStreamType.Audio && stream.Codec == "aac");
        Assert.Equal("MediaForge", videoInfo.Metadata!["artist"]);
        Assert.Equal("Generated sample", videoInfo.Metadata["title"]);
        Assert.Equal("Test collection", videoInfo.Metadata["album"]);

        var audioOutput = await ConvertAsync(input, "audio-output.m4a", ConversionProfile.CreateDefault() with
        {
            OutputContainer = "m4a",
            Video = VideoEncodingSettings.Exclude
        }, ffmpegPath, probe, process);
        var audioInfo = await probe.ProbeAsync(audioOutput);
        Assert.DoesNotContain(audioInfo.Streams, stream => stream.Type == MediaStreamType.Video);
        Assert.Contains(audioInfo.Streams, stream => stream.Type == MediaStreamType.Audio && stream.Codec == "aac");

        var mp3Output = await ConvertAsync(audioInfo, "audio-output.mp3", ConversionProfile.CreateDefault() with
        {
            OutputContainer = "mp3",
            Video = VideoEncodingSettings.Exclude,
            Audio = new AudioEncodingSettings(StreamProcessingMode.Encode, "libmp3lame", 44100, 2, 128)
        }, ffmpegPath, probe, process);
        var mp3Info = await probe.ProbeAsync(mp3Output);
        Assert.DoesNotContain(mp3Info.Streams, stream => stream.Type == MediaStreamType.Video);
        Assert.Contains(mp3Info.Streams, stream => stream.Type == MediaStreamType.Audio && stream.Codec == "mp3");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private async Task<string> ConvertAsync(
        MediaSourceInfo source,
        string outputFileName,
        ConversionProfile profile,
        string ffmpegPath,
        IMediaProbeService probe,
        IFfmpegProcessRunner process)
    {
        var outputPath = Path.Combine(_root, outputFileName);
        var temporaryPath = TemporaryOutputPathFactory.Create(outputPath, Guid.NewGuid());
        var plan = new FfmpegCommandBuilder().Build(new ConversionJobSpec(source, outputPath, temporaryPath, profile, DateTimeOffset.UtcNow));
        var result = await new FfmpegConversionRunner(process, new OutputFileCommitter()).RunAsync(
            ffmpegPath, plan, OutputConflictPolicy.AutoRename);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.NotNull(result.OutputPath);
        var output = await probe.ProbeAsync(result.OutputPath!);
        Assert.NotEmpty(output.Streams);
        return result.OutputPath!;
    }

    private static string? FindToolDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ffmpeg", "ffmpeg.exe")) &&
                File.Exists(Path.Combine(directory.FullName, "ffmpeg", "ffprobe.exe")))
            {
                return Path.Combine(directory.FullName, "ffmpeg");
            }
        }
        return null;
    }
}
