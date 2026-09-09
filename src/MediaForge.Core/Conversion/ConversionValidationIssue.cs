namespace MediaForge.Core.Conversion;

public sealed record ConversionValidationIssue(
    string Code,
    string Field,
    ValidationSeverity Severity,
    string MessageKey,
    string? TechnicalDetail = null);
