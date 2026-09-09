namespace MediaForge.Core.Ffmpeg;

/// <summary>Checks selected conversion features against the current FFmpeg build before execution.</summary>
public sealed class FfmpegFeatureGuard
{
    public FfmpegFeatureSupport Check(FfmpegCapabilities capabilities, FfmpegFeatureRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(requirement);

        var available = requirement.Kind switch
        {
            FfmpegFeatureKind.Encoder => capabilities.Encoders,
            FfmpegFeatureKind.Muxer => capabilities.Muxers,
            FfmpegFeatureKind.Filter => capabilities.Filters,
            _ => throw new ArgumentOutOfRangeException(nameof(requirement))
        };

        return available.Contains(requirement.CapabilityName, StringComparer.OrdinalIgnoreCase)
            ? FfmpegFeatureSupport.Supported
            : new FfmpegFeatureSupport(
                false,
                requirement.CapabilityName,
                $"{requirement.FeatureName} requires '{requirement.CapabilityName}', which is unavailable in this FFmpeg build. Update or replace FFmpeg before starting this task.");
    }
}
