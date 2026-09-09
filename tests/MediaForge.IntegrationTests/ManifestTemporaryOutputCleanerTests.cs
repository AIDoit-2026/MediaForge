using MediaForge.Core.Conversion;
using MediaForge.Core.Media;
using MediaForge.Infrastructure.Output;

namespace MediaForge.IntegrationTests;

public sealed class ManifestTemporaryOutputCleanerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "MediaForge.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Cleanup_deletes_only_the_exact_marked_temporary_path_from_the_manifest()
    {
        Directory.CreateDirectory(_root);
        var output = Path.Combine(_root, "out.mp4");
        var temporary = TemporaryOutputPathFactory.Create(output, Guid.NewGuid());
        var unrelated = Path.Combine(_root, ".unrelated.mediaforge.tmp.mp4");
        File.WriteAllText(temporary, "partial");
        File.WriteAllText(unrelated, "keep");

        var result = Assert.Single(new ManifestTemporaryOutputCleaner().Cleanup([Manifest(output, temporary)]));

        Assert.True(result.Deleted);
        Assert.False(File.Exists(temporary));
        Assert.True(File.Exists(unrelated));
    }

    [Fact]
    public void Cleanup_refuses_a_manifest_temporary_path_outside_its_output_directory()
    {
        var outputDirectory = Path.Combine(_root, "output");
        var otherDirectory = Path.Combine(_root, "other");
        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(otherDirectory);
        var output = Path.Combine(outputDirectory, "out.mp4");
        var temporary = Path.Combine(otherDirectory, ".out.id.mediaforge.tmp.mp4");
        File.WriteAllText(temporary, "must keep");

        var result = Assert.Single(new ManifestTemporaryOutputCleaner().Cleanup([Manifest(output, temporary)]));

        Assert.False(result.Deleted);
        Assert.Equal("OutsideOutputDirectory", result.SkipReason);
        Assert.True(File.Exists(temporary));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static JobManifest Manifest(string output, string temporary) => JobManifest.Create(
        new ConversionJobSpec(
            new MediaSourceInfo("in.mp4", "mp4", null, null, null, []),
            output,
            temporary,
            ConversionProfile.CreateDefault(),
            DateTimeOffset.UtcNow),
        []);
}
