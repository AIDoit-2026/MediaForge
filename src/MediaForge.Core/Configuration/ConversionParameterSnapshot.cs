namespace MediaForge.Core.Configuration;

public sealed record ConversionParameterSnapshot(
    string OutputContainer,
    IReadOnlyDictionary<string, string> Values)
{
    public static ConversionParameterSnapshot CreateDefault() => new("mp4", new Dictionary<string, string>());
}
