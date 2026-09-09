using MediaForge.Core.Conversion;
using MediaForge.Core.Ffmpeg;

namespace MediaForge.Core.Tests;

public sealed class ConversionOptionCatalogTests
{
    [Fact]
    public void Catalog_exposes_only_formats_and_encoders_supported_by_the_current_toolset()
    {
        var capabilities = new FfmpegCapabilities(
            Encoders: ["aac", "h264_nvenc", "libx264", "libopus"],
            Decoders: [],
            Muxers: ["mp4", "webm", "ipod"],
            Demuxers: [],
            Filters: [],
            HardwareAccelerations: []);
        HardwareEncoderAvailability[] hardware = [new HardwareEncoderAvailability(HardwareEncoderKind.NvidiaNvenc, "h264_nvenc", true, true, null)];

        var options = ConversionOptionCatalog.Create(capabilities, hardware);

        Assert.Equal(["m4a", "mp4", "webm"], options.Formats.Select(format => format.Id).Order());
        Assert.Equal(["h264_nvenc", "libx264"], options.AvailableVideoEncoders);
        Assert.Equal(["aac", "libopus"], options.AvailableAudioEncoders);
        Assert.Equal(["libx264", "h264_nvenc"], options.Formats.Single(format => format.Id == "mp4").VideoEncoders);
        Assert.Empty(options.Formats.Single(format => format.Id == "webm").VideoEncoders);
    }

    [Fact]
    public void Catalog_hides_declared_hardware_encoder_when_the_smoke_test_failed()
    {
        var capabilities = new FfmpegCapabilities(
            Encoders: ["aac", "h264_nvenc", "libx264"], Decoders: [], Muxers: ["mp4"], Demuxers: [], Filters: [], HardwareAccelerations: []);
        HardwareEncoderAvailability[] hardware = [new HardwareEncoderAvailability(HardwareEncoderKind.NvidiaNvenc, "h264_nvenc", true, false, "Driver error")];

        var options = ConversionOptionCatalog.Create(capabilities, hardware);

        Assert.Equal(["m4a", "mp4"], options.Formats.Select(format => format.Id).Order());
        Assert.Equal(["libx264"], options.Formats.Single(format => format.Id == "mp4").VideoEncoders);
    }
}
