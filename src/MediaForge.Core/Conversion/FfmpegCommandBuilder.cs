using System.Globalization;

namespace MediaForge.Core.Conversion;

public sealed class FfmpegCommandBuilder : ICommandBuilder
{
    public FfmpegCommandPlan Build(ConversionJobSpec job)
    {
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobPaths(job);

        return job.Profile.Video.TwoPass
            ? new FfmpegCommandPlan([CreatePassOneInvocation(job), CreateFinalInvocation(job, pass: 2)])
            : new FfmpegCommandPlan([CreateFinalInvocation(job, pass: null)]);
    }

    private static FfmpegInvocation CreatePassOneInvocation(ConversionJobSpec job)
    {
        var arguments = CreateBaseArguments(job);
        AddTimeRange(arguments, job.Profile.TimeRange);
        AddVideoArguments(arguments, job.Profile.Video, job.Profile.Filters, job.Profile.ExternalSrtPath, pass: 1, job.TwoPassLogFilePrefix);
        arguments.Add("-an");
        arguments.Add("-f");
        arguments.Add("null");
        arguments.Add("NUL");
        return CreateInvocation(arguments);
    }

    private static FfmpegInvocation CreateFinalInvocation(ConversionJobSpec job, int? pass)
    {
        var arguments = CreateBaseArguments(job);
        AddTimeRange(arguments, job.Profile.TimeRange);
        AddStreamMapping(arguments, job.Profile);
        AddVideoArguments(arguments, job.Profile.Video, job.Profile.Filters, job.Profile.ExternalSrtPath, pass, job.TwoPassLogFilePrefix);
        AddAudioArguments(arguments, job.Profile.Audio);
        arguments.Add(job.TemporaryOutputPath);
        return CreateInvocation(arguments);
    }

    private static List<string> CreateBaseArguments(ConversionJobSpec job) =>
    [
        "-hide_banner",
        "-nostdin",
        "-y",
        "-i",
        job.Source.Path
    ];

    private static void AddTimeRange(List<string> arguments, TimeRange? range)
    {
        if (range is null) return;
        arguments.Add("-ss");
        arguments.Add(FormatTime(range.Start));
        if (range.End is { } end)
        {
            arguments.Add("-t");
            arguments.Add(FormatTime(end - range.Start));
        }
    }

    private static void AddStreamMapping(List<string> arguments, ConversionProfile profile)
    {
        if (profile.Video.Mode == StreamProcessingMode.Exclude)
        {
            arguments.Add("-vn");
        }
        else
        {
            arguments.Add("-map");
            arguments.Add("0:v:0?");
        }

        if (profile.Audio.Mode == StreamProcessingMode.Exclude)
        {
            arguments.Add("-an");
        }
        else
        {
            arguments.Add("-map");
            arguments.Add("0:a:0?");
        }

        arguments.Add("-map_metadata");
        arguments.Add("-1");
    }

    private static void AddVideoArguments(
        List<string> arguments,
        VideoEncodingSettings video,
        VideoFilterSettings filters,
        string? externalSrtPath,
        int? pass,
        string passLogFilePrefix)
    {
        if (video.Mode == StreamProcessingMode.Exclude) return;

        arguments.Add("-c:v");
        arguments.Add(video.Mode == StreamProcessingMode.Copy ? "copy" : video.Encoder!);
        if (video.Mode != StreamProcessingMode.Encode) return;

        AddQualityArguments(arguments, video);
        if (!string.IsNullOrWhiteSpace(video.Preset))
        {
            arguments.Add("-preset");
            arguments.Add(video.Preset);
        }

        var filterGraph = BuildVideoFilterGraph(filters, externalSrtPath);
        if (!string.IsNullOrWhiteSpace(filterGraph))
        {
            arguments.Add("-vf");
            arguments.Add(filterGraph);
        }

        if (pass is not null)
        {
            arguments.Add("-pass");
            arguments.Add(pass.Value.ToString(CultureInfo.InvariantCulture));
            arguments.Add("-passlogfile");
            arguments.Add(passLogFilePrefix);
        }
    }

    private static void AddAudioArguments(List<string> arguments, AudioEncodingSettings audio)
    {
        if (audio.Mode == StreamProcessingMode.Exclude) return;

        arguments.Add("-c:a");
        arguments.Add(audio.Mode == StreamProcessingMode.Copy ? "copy" : audio.Encoder!);
        if (audio.Mode != StreamProcessingMode.Encode) return;

        if (audio.SampleRate is { } sampleRate)
        {
            arguments.Add("-ar");
            arguments.Add(sampleRate.ToString(CultureInfo.InvariantCulture));
        }
        if (audio.Channels is { } channels)
        {
            arguments.Add("-ac");
            arguments.Add(channels.ToString(CultureInfo.InvariantCulture));
        }
        if (audio.BitrateKbps is { } bitrateKbps)
        {
            arguments.Add("-b:a");
            arguments.Add($"{bitrateKbps.ToString(CultureInfo.InvariantCulture)}k");
        }
    }

    private static void AddQualityArguments(List<string> arguments, VideoEncodingSettings video)
    {
        if (video.QualityMode == VideoQualityMode.TargetBitrate)
        {
            arguments.Add("-b:v");
            arguments.Add($"{video.BitrateKbps!.Value.ToString(CultureInfo.InvariantCulture)}k");
            return;
        }

        var qualityOption = video.Encoder switch
        {
            var encoder when encoder?.EndsWith("_nvenc", StringComparison.Ordinal) == true => "-cq",
            var encoder when encoder?.EndsWith("_qsv", StringComparison.Ordinal) == true => "-global_quality",
            var encoder when encoder?.EndsWith("_amf", StringComparison.Ordinal) == true => "-qp_i",
            _ => "-crf"
        };
        arguments.Add(qualityOption);
        arguments.Add(video.ConstantQuality!.Value.ToString(CultureInfo.InvariantCulture));
    }

    private static string? BuildVideoFilterGraph(VideoFilterSettings filters, string? externalSrtPath)
    {
        var entries = new List<string>();
        if (filters.Crop is { } crop) entries.Add($"crop={crop.Width}:{crop.Height}:{crop.X}:{crop.Y}");
        if (filters.Scale is { } scale) entries.Add($"scale={scale.Width}:{scale.Height}");
        entries.AddRange(filters.Rotation switch
        {
            VideoRotation.Clockwise90 => ["transpose=1"],
            VideoRotation.CounterClockwise90 => ["transpose=2"],
            VideoRotation.Rotate180 => ["hflip", "vflip"],
            _ => []
        });
        if (filters.Padding is { } padding) entries.Add($"pad={padding.Canvas.Width}:{padding.Canvas.Height}:{padding.X}:{padding.Y}:{padding.Color}");
        if (!string.IsNullOrWhiteSpace(externalSrtPath)) entries.Add($"subtitles=filename='{EscapeFilterValue(externalSrtPath)}'");
        return entries.Count == 0 ? null : string.Join(',', entries);
    }

    private static string EscapeFilterValue(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("'", "\\'", StringComparison.Ordinal)
        .Replace(":", "\\:", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal)
        .Replace("]", "\\]", StringComparison.Ordinal);

    private static FfmpegInvocation CreateInvocation(IReadOnlyList<string> arguments) =>
        new(arguments, "ffmpeg " + string.Join(' ', arguments.Select(QuoteForDisplay)));

    private static string QuoteForDisplay(string argument) =>
        argument.Length == 0 || argument.Any(char.IsWhiteSpace) || argument.Contains('"')
            ? '"' + argument.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + '"'
            : argument;

    private static string FormatTime(TimeSpan value) => value.ToString("c", CultureInfo.InvariantCulture);

    private static void ValidateJobPaths(ConversionJobSpec job)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(job.Source.Path);
        ArgumentException.ThrowIfNullOrWhiteSpace(job.OutputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(job.TemporaryOutputPath);
        ArgumentNullException.ThrowIfNull(job.Profile);
    }
}
