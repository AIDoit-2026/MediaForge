namespace MediaForge.Core.Conversion;

public sealed record FfmpegCommandPlan(IReadOnlyList<FfmpegInvocation> Invocations)
{
    public FfmpegInvocation FinalInvocation => Invocations[^1];
}
