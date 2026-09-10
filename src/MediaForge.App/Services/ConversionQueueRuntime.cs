using MediaForge.Core.Configuration;
using MediaForge.Core.Conversion;
using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Jobs;
using MediaForge.Core.Media;
using MediaForge.Infrastructure.Configuration;
using MediaForge.Infrastructure.Ffmpeg;
using MediaForge.Infrastructure.Output;

namespace MediaForge.App.Services;

/// <summary>Application composition for durable, bounded default conversion jobs.</summary>
public sealed class ConversionQueueRuntime : IDisposable
{
    private readonly IApplicationSettingsStore _settingsStore;
    private readonly IQueueStore _queueStore;
    private readonly IFfmpegToolResolver _toolResolver;
    private readonly IFfmpegCapabilityService _capabilityService;
    private readonly IFfmpegProcessRunner _processRunner;
    private readonly IConversionRunner _conversionRunner;
    private readonly DebouncedQueueStore _debouncedStore;
    private readonly ConversionQueueScheduler _scheduler;

    public ConversionQueueRuntime(
        IApplicationSettingsStore settingsStore,
        IQueueStore queueStore,
        IFfmpegToolResolver toolResolver,
        IFfmpegCapabilityService capabilityService,
        IFfmpegProcessRunner processRunner,
        IConversionRunner conversionRunner)
    {
        _settingsStore = settingsStore;
        _queueStore = queueStore;
        _toolResolver = toolResolver;
        _capabilityService = capabilityService;
        _processRunner = processRunner;
        _conversionRunner = conversionRunner;
        Queue = new ConversionQueueService();
        _debouncedStore = new DebouncedQueueStore(queueStore);
        Queue.Changed += OnQueueChanged;
        _scheduler = new ConversionQueueScheduler(Queue, ExecuteAsync);
    }

    public ConversionQueueService Queue { get; }

    public ConversionQueueSnapshot Snapshot => Queue.GetSnapshot();

    public async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        Queue.Restore(await _queueStore.LoadAsync(cancellationToken));
        var settings = await _settingsStore.LoadAsync(cancellationToken);
        _scheduler.SetMaximumConcurrency(settings.Settings.MaxConcurrentJobs);
    }

    public IReadOnlyList<ConversionJobSnapshot> AddDefaultJobs(IEnumerable<MediaSourceInfo> sources, ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var added = new List<ConversionJobSnapshot>();
        foreach (var source in sources)
        {
            var outputPath = CreateDefaultOutputPath(source.Path, settings.DefaultOutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            var parameters = new ConversionParameterSnapshot("mp4", new Dictionary<string, string>
            {
                ["videoEncoder"] = "libx264",
                ["audioEncoder"] = "aac"
            });
            var job = Queue.Add(source.Path, outputPath, parameters);
            Queue.Queue(job.Id);
            added.Add(job);
        }
        return added;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken);
        _scheduler.SetMaximumConcurrency(settings.Settings.MaxConcurrentJobs);
        foreach (var interrupted in Queue.GetSnapshot().Jobs.Where(job => job.Status == ConversionJobStatus.Interrupted))
        {
            Queue.Retry(interrupted.Id);
        }
        _ = _scheduler.StartQueuedJobsAsync();
    }

    public async Task StopAndPersistAsync(CancellationToken cancellationToken = default)
    {
        await _scheduler.StopAsync();
        await _debouncedStore.FlushAsync(Queue.CreateDocument(), cancellationToken);
    }

    private async Task ExecuteAsync(ConversionJobSnapshot job, CancellationToken cancellationToken)
    {
        var settings = (await _settingsStore.LoadAsync(cancellationToken)).Settings;
        var toolset = _toolResolver.Resolve(settings.FfmpegDirectory).Toolset
            ?? throw new InvalidOperationException("FFmpeg and FFprobe are unavailable.");
        var source = await new FfprobeMediaProbeService(toolset, _processRunner).ProbeAsync(job.InputPath, cancellationToken);
        var profile = ConversionProfile.CreateDefault();
        var capabilities = await _capabilityService.GetAsync(toolset, forceRefresh: false, cancellationToken);
        var validation = new ConversionValidator().Validate(source, profile, capabilities);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", validation.Issues
                .Where(issue => issue.Severity == ValidationSeverity.Error)
                .Select(issue => issue.Code)));
        }

        var spec = new ConversionJobSpec(
            source,
            job.OutputPath,
            TemporaryOutputPathFactory.Create(job.OutputPath, job.Id),
            profile,
            DateTimeOffset.UtcNow,
            job.Id);
        var result = await _conversionRunner.RunAsync(
            toolset.FfmpegPath,
            new FfmpegCommandBuilder().Build(spec),
            settings.OutputConflictPolicy,
            cancellationToken);
        if (result.Skipped)
        {
            throw new ConversionSkippedException();
        }
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.StandardError);
        }
    }

    private void OnQueueChanged(object? sender, ConversionQueueChangedEventArgs args) =>
        _debouncedStore.Schedule(Queue.CreateDocument());

    private static string CreateDefaultOutputPath(string sourcePath, string? outputDirectory)
    {
        var directory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.GetDirectoryName(sourcePath)
            : outputDirectory;
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("The input path has no output directory.");
        }
        return Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(sourcePath)}.converted.mp4");
    }

    public void Dispose()
    {
        Queue.Changed -= OnQueueChanged;
        _debouncedStore.Dispose();
    }
}
