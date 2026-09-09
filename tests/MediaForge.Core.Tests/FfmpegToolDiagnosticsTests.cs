using MediaForge.Core.Ffmpeg;

namespace MediaForge.Core.Tests;

public sealed class FfmpegToolDiagnosticsTests
{
    [Fact]
    public void Missing_tools_has_localized_message_and_action()
    {
        var diagnostic = FfmpegToolDiagnostics.FromResolution(
            FfmpegToolResolution.Failure(FfmpegToolErrorCodes.ToolsNotFound));

        Assert.NotNull(diagnostic);
        Assert.Equal(FfmpegToolErrorCodes.ToolsNotFound, diagnostic.Code);
        Assert.Contains("ffmpeg.exe", diagnostic.ChineseMessage, StringComparison.Ordinal);
        Assert.Contains("Settings", diagnostic.EnglishSuggestedAction, StringComparison.Ordinal);
    }

    [Fact]
    public void Version_failure_preserves_technical_details()
    {
        var diagnostic = FfmpegToolDiagnostics.FromVersionRead(
            FfmpegVersionReadResult.Failure(
                FfmpegVersionReadResult.VersionCommandFailed,
                "Access is denied."));

        Assert.NotNull(diagnostic);
        Assert.Equal("Access is denied.", diagnostic.TechnicalDetails);
        Assert.Contains("权限", diagnostic.ChineseSuggestedAction, StringComparison.Ordinal);
    }
}
