namespace MediaForge.Infrastructure.Output;

public static class TemporaryOutputPathFactory
{
    public static string Create(string outputPath, Guid jobId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentOutOfRangeException.ThrowIfEqual(jobId, Guid.Empty);

        var fullOutputPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullOutputPath)
            ?? throw new InvalidOperationException("The output path has no parent directory.");
        var name = Path.GetFileNameWithoutExtension(fullOutputPath);
        var extension = Path.GetExtension(fullOutputPath);
        return Path.Combine(directory, $".{name}.{jobId:N}.mediaforge.tmp{extension}");
    }
}
