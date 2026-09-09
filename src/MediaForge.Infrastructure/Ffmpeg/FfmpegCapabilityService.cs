using System.Diagnostics;
using MediaForge.Core.Configuration;
using MediaForge.Core.Ffmpeg;
using MediaForge.Infrastructure.Configuration;

namespace MediaForge.Infrastructure.Ffmpeg;

public sealed class FfmpegCapabilityService : IFfmpegCapabilityService
{
    private const string CacheFileName = "ffmpeg-capabilities.json";

    private readonly IApplicationPaths _applicationPaths;
    private readonly AtomicJsonFileStore _jsonStore;

    public FfmpegCapabilityService(IApplicationPaths applicationPaths, AtomicJsonFileStore jsonStore)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
        ArgumentNullException.ThrowIfNull(jsonStore);
        _applicationPaths = applicationPaths;
        _jsonStore = jsonStore;
    }

    public async Task<FfmpegCapabilities> GetAsync(
        FfmpegToolset toolset,
        bool forceRefresh,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolset);

        var lastWriteTimeUtcTicks = File.GetLastWriteTimeUtc(toolset.FfmpegPath).Ticks;
        if (!forceRefresh && _applicationPaths.CanPersist)
        {
            var cached = await _jsonStore.ReadAsync(
                GetCachePath(),
                CreateEmptyCache,
                cancellationToken);

            if (IsCurrent(cached.Value, toolset.FfmpegPath, lastWriteTimeUtcTicks))
            {
                return cached.Value.Capabilities;
            }
        }

        var capabilities = new FfmpegCapabilities(
            Encoders: FfmpegCapabilityOutputParser.ParseCodecNames(
                await RunQueryAsync(toolset.FfmpegPath, "-encoders", cancellationToken)),
            Decoders: FfmpegCapabilityOutputParser.ParseCodecNames(
                await RunQueryAsync(toolset.FfmpegPath, "-decoders", cancellationToken)),
            Muxers: FfmpegCapabilityOutputParser.ParseFormatNames(
                await RunQueryAsync(toolset.FfmpegPath, "-muxers", cancellationToken)),
            Demuxers: FfmpegCapabilityOutputParser.ParseFormatNames(
                await RunQueryAsync(toolset.FfmpegPath, "-demuxers", cancellationToken)),
            Filters: FfmpegCapabilityOutputParser.ParseFilterNames(
                await RunQueryAsync(toolset.FfmpegPath, "-filters", cancellationToken)),
            HardwareAccelerations: FfmpegCapabilityOutputParser.ParseHardwareAccelerationNames(
                await RunQueryAsync(toolset.FfmpegPath, "-hwaccels", cancellationToken)));

        if (_applicationPaths.CanPersist)
        {
            await _jsonStore.WriteAsync(
                GetCachePath(),
                new FfmpegCapabilitiesCacheDocument(
                    FfmpegCapabilitiesCacheDocument.CurrentSchemaVersion,
                    Path.GetFullPath(toolset.FfmpegPath),
                    lastWriteTimeUtcTicks,
                    capabilities),
                cancellationToken);
        }

        return capabilities;
    }

    private static async Task<string> RunQueryAsync(
        string ffmpegPath,
        string queryArgument,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(ffmpegPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.StartInfo.ArgumentList.Add("-hide_banner");
        process.StartInfo.ArgumentList.Add(queryArgument);
        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"FFmpeg {queryArgument} failed: {standardError}");
        }

        return standardOutput;
    }

    private static FfmpegCapabilitiesCacheDocument CreateEmptyCache() => new(
        SchemaVersion: 0,
        FfmpegPath: string.Empty,
        FfmpegLastWriteTimeUtcTicks: 0,
        Capabilities: FfmpegCapabilities.Empty);

    private string GetCachePath() => Path.Combine(
        _applicationPaths.ConfigDirectory,
        "cache",
        CacheFileName);

    private static bool IsCurrent(
        FfmpegCapabilitiesCacheDocument cache,
        string ffmpegPath,
        long lastWriteTimeUtcTicks) =>
        cache.SchemaVersion == FfmpegCapabilitiesCacheDocument.CurrentSchemaVersion &&
        string.Equals(cache.FfmpegPath, Path.GetFullPath(ffmpegPath), StringComparison.OrdinalIgnoreCase) &&
        cache.FfmpegLastWriteTimeUtcTicks == lastWriteTimeUtcTicks;
}
