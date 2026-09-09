using System.Diagnostics;
using MediaForge.Core.Ffmpeg;

namespace MediaForge.Infrastructure.Ffmpeg;

public sealed class HardwareEncoderProbe : IHardwareEncoderProbe
{
    private static readonly (HardwareEncoderKind Kind, string Name)[] Candidates =
    [
        (HardwareEncoderKind.NvidiaNvenc, "h264_nvenc"),
        (HardwareEncoderKind.IntelQsv, "h264_qsv"),
        (HardwareEncoderKind.AmdAmf, "h264_amf")
    ];

    public async Task<IReadOnlyList<HardwareEncoderAvailability>> ProbeAsync(
        FfmpegToolset toolset,
        FfmpegCapabilities capabilities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolset);
        ArgumentNullException.ThrowIfNull(capabilities);

        var declaredEncoders = capabilities.Encoders.ToHashSet(StringComparer.Ordinal);
        var results = new List<HardwareEncoderAvailability>(Candidates.Length);
        foreach (var candidate in Candidates)
        {
            if (!declaredEncoders.Contains(candidate.Name))
            {
                results.Add(new HardwareEncoderAvailability(
                    candidate.Kind,
                    candidate.Name,
                    IsDeclared: false,
                    IsAvailable: false,
                    "The encoder is not present in this FFmpeg build."));
                continue;
            }

            var result = await RunSmokeTestAsync(toolset.FfmpegPath, candidate.Name, cancellationToken);
            results.Add(new HardwareEncoderAvailability(
                candidate.Kind,
                candidate.Name,
                IsDeclared: true,
                IsAvailable: result.ExitCode == 0,
                result.ExitCode == 0 ? null : result.StandardError));
        }

        return results;
    }

    private static async Task<(int ExitCode, string StandardError)> RunSmokeTestAsync(
        string ffmpegPath,
        string encoderName,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(ffmpegPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            }
        };
        process.StartInfo.ArgumentList.Add("-hide_banner");
        process.StartInfo.ArgumentList.Add("-loglevel");
        process.StartInfo.ArgumentList.Add("error");
        process.StartInfo.ArgumentList.Add("-f");
        process.StartInfo.ArgumentList.Add("lavfi");
        process.StartInfo.ArgumentList.Add("-i");
        process.StartInfo.ArgumentList.Add("color=c=black:s=512x512:r=1");
        process.StartInfo.ArgumentList.Add("-frames:v");
        process.StartInfo.ArgumentList.Add("1");
        process.StartInfo.ArgumentList.Add("-c:v");
        process.StartInfo.ArgumentList.Add(encoderName);
        process.StartInfo.ArgumentList.Add("-f");
        process.StartInfo.ArgumentList.Add("null");
        process.StartInfo.ArgumentList.Add("-");
        process.Start();

        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, await standardErrorTask);
    }
}
