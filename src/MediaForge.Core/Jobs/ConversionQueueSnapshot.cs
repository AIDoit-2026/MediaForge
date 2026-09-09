namespace MediaForge.Core.Jobs;

public sealed record ConversionQueueSnapshot(IReadOnlyList<ConversionJobSnapshot> Jobs);
