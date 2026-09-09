using MediaForge.Core.Conversion;
using MediaForge.Core.Media;

namespace MediaForge.Core.Tests;

public sealed class FfmpegCommandBuilderTests
{
    [Fact]
    public void Video_command_uses_argument_list_and_builds_filters_in_documented_order()
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            Video = new VideoEncodingSettings(StreamProcessingMode.Encode, "libx264", VideoQualityMode.ConstantQuality, 21, null, "slow", false),
            Audio = new AudioEncodingSettings(StreamProcessingMode.Encode, "aac", 48000, 2, 192),
            Filters = new VideoFilterSettings(new CropRectangle(1280, 720, 0, 0), new FrameSize(854, 480), VideoRotation.Clockwise90, new PaddingSettings(new FrameSize(720, 854), 120, 0, "black"))
        };

        var invocation = new FfmpegCommandBuilder().Build(Job(profile)).FinalInvocation;

        Assert.Equal("input path.mp4", invocation.Arguments[4]);
        Assert.Contains("-c:v", invocation.Arguments);
        Assert.Contains("libx264", invocation.Arguments);
        Assert.Contains("crop=1280:720:0:0,scale=854:480,transpose=1,pad=720:854:120:0:black", invocation.Arguments);
        Assert.Contains("-crf", invocation.Arguments);
        Assert.Contains("-b:a", invocation.Arguments);
        Assert.Equal("temporary output.mp4", invocation.Arguments[^1]);
        Assert.Contains("\"input path.mp4\"", invocation.DisplayCommand, StringComparison.Ordinal);
    }

    [Fact]
    public void Audio_extraction_maps_only_audio_and_excludes_video()
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            OutputContainer = "m4a",
            Video = VideoEncodingSettings.Exclude
        };

        var arguments = new FfmpegCommandBuilder().Build(Job(profile)).FinalInvocation.Arguments;

        Assert.Contains("-vn", arguments);
        Assert.Contains("0:a:0?", arguments);
        Assert.DoesNotContain("0:v:0?", arguments);
        Assert.Contains("-c:a", arguments);
    }

    [Fact]
    public void Two_pass_bitrate_plan_has_isolated_pass_log_and_null_first_output()
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            Video = new VideoEncodingSettings(StreamProcessingMode.Encode, "libx264", VideoQualityMode.TargetBitrate, null, 2500, null, true)
        };

        var plan = new FfmpegCommandBuilder().Build(Job(profile));

        Assert.Equal(2, plan.Invocations.Count);
        Assert.Equal("1", ValueAfter(plan.Invocations[0].Arguments, "-pass"));
        Assert.Equal("2", ValueAfter(plan.Invocations[1].Arguments, "-pass"));
        Assert.Equal("NUL", plan.Invocations[0].Arguments[^1]);
        Assert.Equal("temporary output.mp4.passlog", ValueAfter(plan.FinalInvocation.Arguments, "-passlogfile"));
    }

    [Fact]
    public void Hardware_constant_quality_uses_the_encoder_specific_cq_option()
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            Video = new VideoEncodingSettings(StreamProcessingMode.Encode, "h264_nvenc", VideoQualityMode.ConstantQuality, 19, null, null, false)
        };

        var arguments = new FfmpegCommandBuilder().Build(Job(profile)).FinalInvocation.Arguments;

        Assert.Equal("19", ValueAfter(arguments, "-cq"));
        Assert.DoesNotContain("-crf", arguments);
    }

    [Fact]
    public void Audio_to_audio_command_disables_video_and_applies_time_range()
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            OutputContainer = "mp3",
            Video = VideoEncodingSettings.Exclude,
            Audio = new AudioEncodingSettings(StreamProcessingMode.Encode, "libmp3lame", 44100, 2, 192),
            TimeRange = new TimeRange(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(40))
        };

        var arguments = new FfmpegCommandBuilder().Build(Job(profile)).FinalInvocation.Arguments;

        Assert.Contains("-vn", arguments);
        Assert.Equal("00:00:10", ValueAfter(arguments, "-ss"));
        Assert.Equal("00:00:30", ValueAfter(arguments, "-t"));
        Assert.Equal("libmp3lame", ValueAfter(arguments, "-c:a"));
    }

    [Fact]
    public void External_srt_is_burned_into_the_video_filter_with_escaped_windows_path()
    {
        var profile = ConversionProfile.CreateDefault() with { ExternalSrtPath = "C:\\字幕 files\\a:b.srt" };

        var arguments = new FfmpegCommandBuilder().Build(Job(profile)).FinalInvocation.Arguments;

        Assert.Contains("subtitles=filename='C\\:\\\\字幕 files\\\\a\\:b.srt'", arguments);
    }

    [Fact]
    public void Plan_includes_a_manifest_with_exact_paths_snapshot_and_execution_arguments()
    {
        var jobId = Guid.NewGuid();
        var profile = ConversionProfile.CreateDefault();
        var job = Job(profile) with { JobId = jobId, PresetId = Guid.NewGuid(), PresetSchemaVersion = 1, FfmpegVersion = "7.1" };

        var plan = new FfmpegCommandBuilder().Build(job);

        Assert.Equal(JobManifest.CurrentSchemaVersion, plan.Manifest.SchemaVersion);
        Assert.Equal(jobId, plan.Manifest.JobId);
        Assert.Equal("input path.mp4", plan.Manifest.InputPath);
        Assert.Equal("final output.mp4", plan.Manifest.OutputPath);
        Assert.Equal("temporary output.mp4", plan.Manifest.TemporaryOutputPath);
        Assert.Equal(profile, plan.Manifest.Profile);
        Assert.Equal("7.1", plan.Manifest.FfmpegVersion);
        Assert.Equal(plan.FinalInvocation.Arguments, plan.Manifest.InvocationArguments[^1]);
    }

    [Fact]
    public void Command_preserves_only_selected_container_metadata()
    {
        var source = new MediaSourceInfo(
            "input.mp4", "mp4", TimeSpan.FromMinutes(1), null, null, [],
            new Dictionary<string, string>
            {
                ["artist"] = "Artist",
                ["title"] = "Title",
                ["album"] = "Album",
                ["comment"] = "Do not copy"
            });
        var job = new ConversionJobSpec(source, "final.mp4", "temporary.mp4", ConversionProfile.CreateDefault(), DateTimeOffset.UtcNow);

        var arguments = new FfmpegCommandBuilder().Build(job).FinalInvocation.Arguments;

        Assert.Equal("-1", ValueAfter(arguments, "-map_metadata"));
        Assert.Contains("artist=Artist", arguments);
        Assert.Contains("title=Title", arguments);
        Assert.Contains("album=Album", arguments);
        Assert.DoesNotContain("comment=Do not copy", arguments);
    }

    [Fact]
    public void Audio_output_preserves_a_compatible_attached_picture_without_mapping_main_video()
    {
        var source = new MediaSourceInfo("input.m4a", "mov", TimeSpan.FromMinutes(1), null, null,
        [
            new MediaStreamInfo(0, MediaStreamType.Audio, "aac", null, null, true),
            new MediaStreamInfo(1, MediaStreamType.Video, "mjpeg", null, null, false, IsAttachedPicture: true)
        ]);
        var profile = ConversionProfile.CreateDefault() with { OutputContainer = "m4a", Video = VideoEncodingSettings.Exclude };

        var arguments = new FfmpegCommandBuilder().Build(new ConversionJobSpec(source, "output.m4a", "temp.m4a", profile, DateTimeOffset.UtcNow)).FinalInvocation.Arguments;

        Assert.Contains("0:1?", arguments);
        Assert.Equal("copy", ValueAfter(arguments, "-c:v"));
        Assert.Equal("attached_pic", ValueAfter(arguments, "-disposition:v:0"));
        Assert.DoesNotContain("-vn", arguments);
    }

    private static ConversionJobSpec Job(ConversionProfile profile) => new(
        new MediaSourceInfo("input path.mp4", "mp4", TimeSpan.FromMinutes(1), null, null, []),
        "final output.mp4", "temporary output.mp4", profile, DateTimeOffset.Parse("2026-09-09T00:00:00Z"));

    private static string ValueAfter(IReadOnlyList<string> arguments, string option)
    {
        var index = arguments.Select((argument, index) => (argument, index))
            .Single(pair => pair.argument == option)
            .index;
        return arguments[index + 1];
    }
}
