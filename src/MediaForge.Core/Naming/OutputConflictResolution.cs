namespace MediaForge.Core.Naming;

public sealed record OutputConflictResolution(string OutputPath, bool ShouldSkip, bool ShouldOverwrite);
