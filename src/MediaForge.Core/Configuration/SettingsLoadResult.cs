namespace MediaForge.Core.Configuration;

public sealed record SettingsLoadResult(ApplicationSettings Settings, bool RecoveredFromCorruption);
