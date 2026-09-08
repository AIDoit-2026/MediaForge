namespace MediaForge.Infrastructure.Configuration;

public sealed record ConfigurationReadResult<T>(T Value, bool RecoveredFromCorruption);
