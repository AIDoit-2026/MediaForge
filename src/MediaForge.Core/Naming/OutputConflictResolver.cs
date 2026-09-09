using MediaForge.Core.Configuration;

namespace MediaForge.Core.Naming;

public static class OutputConflictResolver
{
    public static OutputConflictResolution Resolve(
        string outputPath,
        OutputConflictPolicy policy,
        Func<string, bool> exists)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(exists);
        if (!exists(outputPath)) return new(outputPath, false, false);
        return policy switch
        {
            OutputConflictPolicy.Skip => new(outputPath, true, false),
            OutputConflictPolicy.Overwrite => new(outputPath, false, true),
            OutputConflictPolicy.AutoRename => new(FindAvailablePath(outputPath, exists), false, false),
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };
    }

    private static string FindAvailablePath(string path, Func<string, bool> exists)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (var index = 1; ; index++)
        {
            var candidate = Path.Combine(directory, $"{name} ({index}){extension}");
            if (!exists(candidate)) return candidate;
        }
    }
}
