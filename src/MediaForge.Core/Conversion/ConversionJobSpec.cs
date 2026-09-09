using MediaForge.Core.Media;

namespace MediaForge.Core.Conversion;

/// <summary>
/// Immutable, already-validated input to command generation. Output paths are always explicit;
/// the runner writes to <see cref="TemporaryOutputPath"/> and commits separately.
/// </summary>
public sealed record ConversionJobSpec(
    MediaSourceInfo Source,
    string OutputPath,
    string TemporaryOutputPath,
    ConversionProfile Profile,
    DateTimeOffset CreatedAt)
{
    public string TwoPassLogFilePrefix => TemporaryOutputPath + ".passlog";
}
