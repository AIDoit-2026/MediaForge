using MediaForge.Core.Configuration;
using MediaForge.Core.Naming;

namespace MediaForge.Core.Tests;

public sealed class OutputConflictResolverTests
{
    [Fact]
    public void Resolve_auto_renames_to_the_first_available_name()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "out.mp4", "out (1).mp4" };
        var result = OutputConflictResolver.Resolve("out.mp4", OutputConflictPolicy.AutoRename, existing.Contains);
        Assert.Equal("out (2).mp4", result.OutputPath);
        Assert.False(result.ShouldSkip);
    }

    [Fact]
    public void Resolve_skip_and_overwrite_preserve_the_requested_target()
    {
        var skip = OutputConflictResolver.Resolve("out.mp4", OutputConflictPolicy.Skip, _ => true);
        var overwrite = OutputConflictResolver.Resolve("out.mp4", OutputConflictPolicy.Overwrite, _ => true);
        Assert.True(skip.ShouldSkip);
        Assert.True(overwrite.ShouldOverwrite);
    }
}
