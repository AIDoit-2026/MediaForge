namespace MediaForge.Core.Conversion;

public sealed record FfmpegInvocation(IReadOnlyList<string> Arguments, string DisplayCommand);
