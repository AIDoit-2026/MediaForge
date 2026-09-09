using MediaForge.Core.Configuration;
using MediaForge.Infrastructure.Output;

namespace MediaForge.IntegrationTests;

public sealed class OutputFileCommitterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "MediaForge.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Commit_auto_renames_without_replacing_existing_output()
    {
        Directory.CreateDirectory(_root);
        var output = Path.Combine(_root, "out.mp4");
        var temporary = Path.Combine(_root, ".out.tmp");
        File.WriteAllText(output, "old");
        File.WriteAllText(temporary, "new");

        var result = new OutputFileCommitter().Commit(temporary, output, OutputConflictPolicy.AutoRename);

        Assert.EndsWith("out (1).mp4", result.OutputPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("old", File.ReadAllText(output));
        Assert.Equal("new", File.ReadAllText(result.OutputPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
