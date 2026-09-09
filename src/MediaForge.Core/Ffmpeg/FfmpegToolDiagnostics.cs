namespace MediaForge.Core.Ffmpeg;

public static class FfmpegToolDiagnostics
{
    public static FfmpegToolDiagnostic? FromResolution(FfmpegToolResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        return resolution.IsSuccess ? null : Create(resolution.ErrorCode!, null);
    }

    public static FfmpegToolDiagnostic? FromVersionRead(FfmpegVersionReadResult versionRead)
    {
        ArgumentNullException.ThrowIfNull(versionRead);
        return versionRead.IsSuccess ? null : Create(versionRead.ErrorCode!, versionRead.Details);
    }

    private static FfmpegToolDiagnostic Create(string code, string? technicalDetails) => code switch
    {
        FfmpegToolErrorCodes.ToolsNotFound => new(
            code,
            "未找到 ffmpeg.exe 和 ffprobe.exe。",
            "ffmpeg.exe and ffprobe.exe were not found.",
            "请将两个程序放到 MediaForge.exe 同级目录，或在设置中指定它们所在的目录。",
            "Put both programs next to MediaForge.exe, or select their directory in Settings.",
            technicalDetails),
        FfmpegVersionReadResult.VersionCommandTimedOut => new(
            code,
            "读取 FFmpeg 版本超时。",
            "Reading the FFmpeg version timed out.",
            "请检查所选工具是否可执行，或更换为完整的 FFmpeg 构建。",
            "Check that the selected tool can run, or replace it with a complete FFmpeg build.",
            technicalDetails),
        FfmpegVersionReadResult.UnrecognizedVersion => new(
            code,
            "所选程序没有返回可识别的 FFmpeg 版本信息。",
            "The selected program did not return a recognizable FFmpeg version.",
            "请确认目录中的 ffmpeg.exe 来自受支持的 FFmpeg 构建。",
            "Confirm that ffmpeg.exe in the selected directory is from a supported FFmpeg build.",
            technicalDetails),
        _ => new(
            code,
            "无法运行所选的 FFmpeg 工具。",
            "The selected FFmpeg tool could not be run.",
            "请检查文件路径、访问权限和工具完整性。",
            "Check the file path, access permissions, and tool integrity.",
            technicalDetails)
    };
}
