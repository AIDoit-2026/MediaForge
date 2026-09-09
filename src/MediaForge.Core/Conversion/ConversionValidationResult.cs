namespace MediaForge.Core.Conversion;

public sealed record ConversionValidationResult(IReadOnlyList<ConversionValidationIssue> Issues)
{
    public bool IsValid => Issues.All(issue => issue.Severity != ValidationSeverity.Error);

    public IReadOnlyList<ConversionValidationIssue> Errors =>
        Issues.Where(issue => issue.Severity == ValidationSeverity.Error).ToArray();

    public IReadOnlyList<ConversionValidationIssue> Warnings =>
        Issues.Where(issue => issue.Severity == ValidationSeverity.Warning).ToArray();
}
