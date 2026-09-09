namespace MediaForge.Core.Jobs;

public sealed class ConversionQueueChangedEventArgs(ConversionQueueSnapshot snapshot) : EventArgs
{
    public ConversionQueueSnapshot Snapshot { get; } = snapshot;
}
