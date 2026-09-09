using MediaForge.Infrastructure.Ffmpeg;

namespace MediaForge.IntegrationTests;

public sealed class FfmpegCapabilityOutputParserTests
{
    [Fact]
    public void Parse_codec_names_uses_only_data_rows()
    {
        var names = FfmpegCapabilityOutputParser.ParseCodecNames("""
            Encoders:
             V..... h264_nvenc          NVIDIA NVENC H.264 encoder
             A....D aac                 AAC
            """);

        Assert.Equal(["aac", "h264_nvenc"], names);
    }

    [Fact]
    public void Parse_format_names_and_filters_extract_the_capability_name()
    {
        var formats = FfmpegCapabilityOutputParser.ParseFormatNames("""
            Formats:
             DE mp4             MP4 (MPEG-4 Part 14)
              E matroska        Matroska
            """);
        var filters = FfmpegCapabilityOutputParser.ParseFilterNames("""
            Filters:
             TSC scale           V->V       Scale the input video size and/or convert the image format.
             ... anull           A->A       Pass the source unchanged to the output.
            """);

        Assert.Equal(["matroska", "mp4"], formats);
        Assert.Equal(["anull", "scale"], filters);
    }

    [Fact]
    public void Parse_hardware_accelerations_skips_the_heading()
    {
        var names = FfmpegCapabilityOutputParser.ParseHardwareAccelerationNames("""
            Hardware acceleration methods:
            cuda
            d3d11va
            qsv
            """);

        Assert.Equal(["cuda", "d3d11va", "qsv"], names);
    }
}
