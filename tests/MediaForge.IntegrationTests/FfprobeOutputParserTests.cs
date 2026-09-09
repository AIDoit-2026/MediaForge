using MediaForge.Core.Media;
using MediaForge.Infrastructure.Ffmpeg;

namespace MediaForge.IntegrationTests;

public sealed class FfprobeOutputParserTests
{
    [Fact]
    public void Parse_maps_format_and_multiple_stream_types()
    {
        var source = FfprobeOutputParser.Parse("sample.mkv", """
            {"format":{"format_name":"matroska,webm","duration":"2.5","size":"1234","bit_rate":"4567","tags":{"title":"Example","artist":"MediaForge"}},"streams":[
            {"index":0,"codec_type":"video","codec_name":"h264","width":1920,"height":1080,"avg_frame_rate":"30000/1001","pix_fmt":"yuv420p","disposition":{"default":1},"tags":{"title":"Video"}},
            {"index":1,"codec_type":"audio","codec_name":"aac","sample_rate":"48000","channels":2,"channel_layout":"stereo","bit_rate":"192000","tags":{"language":"eng"}},
            {"index":2,"codec_type":"subtitle","codec_name":"subrip","tags":{"language":"chi"}},
            {"index":3,"codec_type":"video","codec_name":"mjpeg","disposition":{"attached_pic":1}}
            ]}
            """);

        Assert.Equal("matroska,webm", source.Container);
        Assert.Equal(TimeSpan.FromSeconds(2.5), source.Duration);
        Assert.Equal(1234, source.Size);
        Assert.Equal("Example", source.Metadata!["title"]);
        Assert.Equal("MediaForge", source.Metadata["artist"]);
        Assert.Collection(source.Streams,
            video => { Assert.Equal(MediaStreamType.Video, video.Type); Assert.Equal(1920, video.Width); Assert.True(video.IsDefault); },
            audio => { Assert.Equal(MediaStreamType.Audio, audio.Type); Assert.Equal("eng", audio.Language); Assert.Equal(48000, audio.SampleRate); },
            subtitle => Assert.Equal(MediaStreamType.Subtitle, subtitle.Type),
            cover => Assert.True(cover.IsAttachedPicture));
    }
}
