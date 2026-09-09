using MediaForge.Core.Media;

namespace MediaForge.Core.Importing;

public sealed record MediaImportItem(string Path, MediaSourceInfo? Media, string? Error);
