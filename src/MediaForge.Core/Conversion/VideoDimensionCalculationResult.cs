namespace MediaForge.Core.Conversion;

public sealed record VideoDimensionCalculationResult(
    FrameSize? OutputSize,
    IReadOnlyList<VideoDimensionIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}
