using MediaForge.Core.Configuration;
using MediaForge.Core.Naming;

namespace MediaForge.Infrastructure.Output;

public sealed class OutputFileCommitter
{
    public OutputConflictResolution Commit(string temporaryPath, string outputPath, OutputConflictPolicy policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var temporaryFullPath = Path.GetFullPath(temporaryPath);
        var outputFullPath = Path.GetFullPath(outputPath);
        if (!string.Equals(Path.GetDirectoryName(temporaryFullPath), Path.GetDirectoryName(outputFullPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Temporary and final output must be in the same directory.");
        }

        for (;;)
        {
            var resolution = OutputConflictResolver.Resolve(outputFullPath, policy, File.Exists);
            if (resolution.ShouldSkip) return resolution;
            if (resolution.ShouldOverwrite)
            {
                File.Move(temporaryFullPath, outputFullPath, overwrite: true);
                return resolution;
            }

            try
            {
                File.Move(temporaryFullPath, resolution.OutputPath, overwrite: false);
                return resolution;
            }
            catch (IOException) when (policy == OutputConflictPolicy.AutoRename)
            {
                // Another process claimed this candidate between the check and the move.
            }
        }
    }
}
