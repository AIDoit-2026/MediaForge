namespace MediaForge.Core.Ffmpeg;

public sealed record FfmpegToolset(
    string Directory,
    string FfmpegPath,
    string FfprobePath,
    FfmpegToolSource Source);
