using MediaForge.Core.Ffmpeg;
using MediaForge.Infrastructure.Ffmpeg;

namespace MediaForge.IntegrationTests;

public sealed class HardwareEncoderProbeTests
{
    [Fact]
    public async Task Probe_marks_encoders_missing_from_the_build_as_unavailable_without_starting_a_process()
    {
        var toolset = new FfmpegToolset(
            "unused",
            "missing.exe",
            "missing-probe.exe",
            FfmpegToolSource.ConfiguredDirectory);

        var results = await new HardwareEncoderProbe().ProbeAsync(toolset, FfmpegCapabilities.Empty);

        Assert.Collection(
            results,
            nvenc => Assert.False(nvenc.IsDeclared),
            qsv => Assert.False(qsv.IsDeclared),
            amf => Assert.False(amf.IsDeclared));
    }
}
