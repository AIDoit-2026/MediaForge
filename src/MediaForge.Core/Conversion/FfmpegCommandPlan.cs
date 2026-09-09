namespace MediaForge.Core.Conversion;

public sealed record FfmpegCommandPlan(IReadOnlyList<FfmpegInvocation> Invocations, JobManifest Manifest)
{
    public FfmpegInvocation FinalInvocation => Invocations[^1];
}
