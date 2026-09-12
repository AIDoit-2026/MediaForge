using MediaForge.Core.Configuration;
using MediaForge.Core.Conversion;

namespace MediaForge.Core.Tests;

public sealed class ConversionProfileFactoryTests
{
    [Fact]
    public void Default_preset_maps_to_libx264_aac_mp4_with_constant_quality()
    {
        var snapshot = new ConversionParameterSnapshot("mp4", new Dictionary<string, string>
        {
            ["videoEncoder"] = "libx264",
            ["audioEncoder"] = "aac",
            ["qualityMode"] = "crf",
            ["crf"] = "23"
        });

        var profile = ConversionProfileFactory.Create(snapshot);

        Assert.Equal("mp4", profile.OutputContainer);
        Assert.Equal(StreamProcessingMode.Encode, profile.Video.Mode);
        Assert.Equal("libx264", profile.Video.Encoder);
        Assert.Equal(VideoQualityMode.ConstantQuality, profile.Video.QualityMode);
        Assert.Equal(23, profile.Video.ConstantQuality);
        Assert.Equal("aac", profile.Audio.Encoder);
    }

    [Fact]
    public void Audio_only_preset_excludes_video_and_carries_bitrate()
    {
        var snapshot = new ConversionParameterSnapshot("m4a", new Dictionary<string, string>
        {
            ["videoMode"] = "none",
            ["audioEncoder"] = "aac",
            ["audioBitrateKbps"] = "192"
        });

        var profile = ConversionProfileFactory.Create(snapshot);

        Assert.Equal(StreamProcessingMode.Exclude, profile.Video.Mode);
        Assert.Equal("aac", profile.Audio.Encoder);
        Assert.Equal(192, profile.Audio.BitrateKbps);
    }

    [Fact]
    public void Bitrate_preset_maps_to_target_bitrate_video()
    {
        var snapshot = new ConversionParameterSnapshot("mp4", new Dictionary<string, string>
        {
            ["videoEncoder"] = "libx264",
            ["audioEncoder"] = "aac",
            ["qualityMode"] = "bitrate",
            ["videoBitrateKbps"] = "4500"
        });

        var profile = ConversionProfileFactory.Create(snapshot);

        Assert.Equal(VideoQualityMode.TargetBitrate, profile.Video.QualityMode);
        Assert.Equal(4500, profile.Video.BitrateKbps);
        Assert.Null(profile.Video.ConstantQuality);
    }

    [Fact]
    public void Snapshot_without_encoders_falls_back_to_stream_copy()
    {
        var snapshot = new ConversionParameterSnapshot("mp4", new Dictionary<string, string>());

        var profile = ConversionProfileFactory.Create(snapshot);

        Assert.Equal(StreamProcessingMode.Copy, profile.Video.Mode);
        Assert.Equal(StreamProcessingMode.Copy, profile.Audio.Mode);
    }

    [Fact]
    public void Built_in_presets_all_map_to_valid_profiles()
    {
        foreach (var preset in BuiltInPresetCatalog.All)
        {
            var profile = ConversionProfileFactory.Create(preset.Parameters);
            Assert.Equal(preset.Parameters.OutputContainer, profile.OutputContainer);
        }
    }
}
