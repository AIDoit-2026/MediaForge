using MediaForge.Core.Conversion;

namespace MediaForge.Core.Tests;

public sealed class ConversionProfileTests
{
    [Fact]
    public void Default_profile_describes_an_h264_aac_mp4_conversion()
    {
        var profile = ConversionProfile.CreateDefault();

        Assert.Equal("mp4", profile.OutputContainer);
        Assert.Equal(StreamProcessingMode.Encode, profile.Video.Mode);
        Assert.Equal("libx264", profile.Video.Encoder);
        Assert.Equal(VideoQualityMode.ConstantQuality, profile.Video.QualityMode);
        Assert.Equal(StreamProcessingMode.Encode, profile.Audio.Mode);
        Assert.Equal("aac", profile.Audio.Encoder);
        Assert.Equal(VideoRotation.None, profile.Filters.Rotation);
    }

    [Fact]
    public void Profile_can_represent_an_audio_only_time_range_with_external_subtitles_absent()
    {
        var profile = ConversionProfile.CreateDefault() with
        {
            OutputContainer = "m4a",
            Video = VideoEncodingSettings.Exclude,
            Audio = AudioEncodingSettings.EncodeWith("aac"),
            TimeRange = new TimeRange(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(40))
        };

        Assert.Equal(StreamProcessingMode.Exclude, profile.Video.Mode);
        Assert.Equal(TimeSpan.FromSeconds(10), profile.TimeRange!.Start);
        Assert.Null(profile.ExternalSrtPath);
    }
}
