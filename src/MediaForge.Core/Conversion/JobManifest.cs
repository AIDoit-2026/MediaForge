namespace MediaForge.Core.Conversion;

/// <summary>
/// Durable task evidence used for diagnostics and exact temporary-file cleanup.
/// It never contains a shell command; each invocation remains an argument list.
/// </summary>
public sealed record JobManifest(
    int SchemaVersion,
    Guid JobId,
    string InputPath,
    string OutputPath,
    string TemporaryOutputPath,
    ConversionProfile Profile,
    Guid? PresetId,
    int? PresetSchemaVersion,
    string? FfmpegVersion,
    DateTimeOffset CreatedAt,
    IReadOnlyList<IReadOnlyList<string>> InvocationArguments)
{
    public const int CurrentSchemaVersion = 1;

    public static JobManifest Create(ConversionJobSpec job, IReadOnlyList<FfmpegInvocation> invocations) => new(
        CurrentSchemaVersion,
        job.JobId == Guid.Empty ? Guid.NewGuid() : job.JobId,
        job.Source.Path,
        job.OutputPath,
        job.TemporaryOutputPath,
        job.Profile,
        job.PresetId,
        job.PresetSchemaVersion,
        job.FfmpegVersion,
        job.CreatedAt,
        invocations.Select(invocation => invocation.Arguments).ToArray());
}
