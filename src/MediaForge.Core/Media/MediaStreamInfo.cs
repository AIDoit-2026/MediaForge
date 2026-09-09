namespace MediaForge.Core.Media;

public sealed record MediaStreamInfo(
    int Index,
    MediaStreamType Type,
    string Codec,
    string? Language,
    string? Title,
    bool IsDefault,
    int? Width = null,
    int? Height = null,
    string? FrameRate = null,
    string? PixelFormat = null,
    int? SampleRate = null,
    int? Channels = null,
    string? ChannelLayout = null,
    long? BitRate = null,
    bool IsAttachedPicture = false);
