namespace MediaForge.Core.Configuration;

public interface IApplicationPaths
{
    string BaseDirectory { get; }

    string ConfigDirectory { get; }

    bool CanPersist { get; }

    string? PersistenceWarningCode { get; }
}
