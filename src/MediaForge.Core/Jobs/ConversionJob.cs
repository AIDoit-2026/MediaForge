namespace MediaForge.Core.Jobs;

public sealed class ConversionJob
{
    public ConversionJob(Guid id, string inputPath, string outputPath, ConversionJobStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        Id = id;
        InputPath = inputPath;
        OutputPath = outputPath;
        Status = status;
    }

    public Guid Id { get; }

    public string InputPath { get; }

    public string OutputPath { get; }

    public ConversionJobStatus Status { get; private set; }

    public void Queue() => TransitionTo(
        ConversionJobStatus.Queued,
        ConversionJobStatus.Ready,
        ConversionJobStatus.Interrupted,
        ConversionJobStatus.Failed);

    public void Start() => TransitionTo(ConversionJobStatus.Running, ConversionJobStatus.Queued);

    public void Complete() => TransitionTo(ConversionJobStatus.Succeeded, ConversionJobStatus.Running);

    public void Fail() => TransitionTo(ConversionJobStatus.Failed, ConversionJobStatus.Running);

    public void RestoreAfterApplicationStart()
    {
        if (Status == ConversionJobStatus.Running)
        {
            Status = ConversionJobStatus.Interrupted;
        }
    }

    private void TransitionTo(ConversionJobStatus target, params ConversionJobStatus[] allowedSources)
    {
        if (!allowedSources.Contains(Status))
        {
            throw new InvalidOperationException($"Cannot transition conversion job from {Status} to {target}.");
        }

        Status = target;
    }
}
