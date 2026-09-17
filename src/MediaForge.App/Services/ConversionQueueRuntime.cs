using MediaForge.Core.Configuration;
using MediaForge.Core.Conversion;
using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Jobs;
using MediaForge.Core.Media;
using MediaForge.Infrastructure.Configuration;
using MediaForge.Infrastructure.Ffmpeg;
using MediaForge.Infrastructure.Output;
using System.Runtime.ExceptionServices;

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

    public event EventHandler<ConversionProgressChangedEventArgs>? ProgressChanged;

    public ConversionQueueSnapshot Snapshot => Queue.GetSnapshot();

    public PresetDocument? SelectedPreset { get; private set; }

    public void SetSelectedPreset(PresetDocument? preset)
    {
        SelectedPreset = preset;
    }

    public ConversionParameterSnapshot CreateJobParameters() =>
        SelectedPreset?.Parameters ?? new ConversionParameterSnapshot("mp4", new Dictionary<string, string>
        {
            ["videoEncoder"] = "libx264",
            ["audioEncoder"] = "aac"
        });

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
            var outputPath = CreateDefaultOutputPath(
                source.Path,
                settings,
                CreateJobParameters().OutputContainer);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            var job = Queue.Add(source.Path, outputPath, CreateJobParameters());
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
        Exception? stopFailure = null;
        try
        {
            await _scheduler.StopAsync();
        }
        catch (TimeoutException error)
        {
            stopFailure = error;
        }

        await _debouncedStore.FlushAsync(Queue.CreateDocument(), cancellationToken);
        if (stopFailure is not null)
        {
            ExceptionDispatchInfo.Capture(stopFailure).Throw();
        }
    }

    private async Task ExecuteAsync(ConversionJobSnapshot job, CancellationToken cancellationToken)
    {
        try
        {
            var settings = (await _settingsStore.LoadAsync(cancellationToken)).Settings;
            var toolset = _toolResolver.Resolve(settings.FfmpegDirectory).Toolset
                ?? throw new InvalidOperationException("FFmpeg and FFprobe are unavailable.");
            var source = await new FfprobeMediaProbeService(toolset, _processRunner).ProbeAsync(job.InputPath, cancellationToken);
            var profile = ConversionProfileFactory.Create(job.Parameters);
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
            var plan = new FfmpegCommandBuilder().Build(spec);
            var startedAt = DateTimeOffset.UtcNow;
            var reporter = new Progress<FfmpegProgressUpdate>(update => ReportProgress(job.Id, source.Duration, startedAt, update));
            var result = _conversionRunner is IConversionProgressRunner progressRunner
                ? await progressRunner.RunWithProgressAsync(toolset.FfmpegPath, plan, settings.OutputConflictPolicy, reporter, cancellationToken)
                : await _conversionRunner.RunAsync(toolset.FfmpegPath, plan, settings.OutputConflictPolicy, cancellationToken);
            if (result.Skipped)
            {
                throw new ConversionSkippedException();
            }
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.StandardError);
            }
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            App.ShowError("Conversion failed", error.Message);
            throw;
        }
    }

    private void OnQueueChanged(object? sender, ConversionQueueChangedEventArgs args) =>
        _debouncedStore.Schedule(Queue.CreateDocument());

    private void ReportProgress(Guid jobId, TimeSpan? sourceDuration, DateTimeOffset startedAt, FfmpegProgressUpdate update)
    {
        double? percentage = sourceDuration is { Ticks: > 0 } && update.OutputTime is { } outputTime
            ? Math.Clamp(outputTime.TotalMilliseconds / sourceDuration.Value.TotalMilliseconds * 100, 0, 100)
            : null;
        var now = DateTimeOffset.UtcNow;
        TimeSpan? remaining = sourceDuration is { } duration && update.OutputTime is { } latestOutputTime && update.Speed is > 0
            ? TimeSpan.FromSeconds(Math.Max(0, (duration - latestOutputTime).TotalSeconds / update.Speed.Value))
            : null;
        ProgressChanged?.Invoke(this, new ConversionProgressChangedEventArgs(new ConversionProgressSnapshot(
            jobId, percentage, update.OutputTime, update.Speed, now - startedAt, remaining, now)));
    }

    private static string CreateDefaultOutputPath(string sourcePath, ApplicationSettings settings, string container)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath);
        var directory = settings.OutputDirectoryMode switch
        {
            OutputDirectoryMode.SourceSiblingOutputDirectory when !string.IsNullOrWhiteSpace(sourceDirectory) =>
                Path.Combine(sourceDirectory, "output"),
            OutputDirectoryMode.SettingsDefaultDirectory => settings.DefaultOutputDirectory,
            OutputDirectoryMode.MainPageDirectory => settings.MainPageOutputDirectory,
            _ => null
        };
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Choose an output directory for the selected output mode.");
        }
        return Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(sourcePath)}.converted.{container}");
    }

    public void Dispose()
    {
        Queue.Changed -= OnQueueChanged;
        _debouncedStore.Dispose();
    }
}
