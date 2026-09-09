using MediaForge.Core.Configuration;

namespace MediaForge.Core.Jobs;

public sealed record ConversionJobSnapshot(
    Guid Id,
    string InputPath,
    string OutputPath,
    ConversionJobStatus Status,
    ConversionParameterSnapshot Parameters)
{
    public static ConversionJobSnapshot From(ConversionJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return new ConversionJobSnapshot(job.Id, job.InputPath, job.OutputPath, job.Status, job.Parameters);
    }
}
