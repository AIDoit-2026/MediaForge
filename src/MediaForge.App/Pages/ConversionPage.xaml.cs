using System.Collections.ObjectModel;
using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Importing;
using MediaForge.Infrastructure.Ffmpeg;
using MediaForge.Infrastructure.Importing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MediaForge.App.Pages;

public sealed partial class ConversionPage : Page
{
    private readonly ObservableCollection<ImportedMediaRow> _media = [];

    public ConversionPage()
    {
        InitializeComponent();
        ApplyStrings();
        MediaListView.ItemsSource = _media;
        foreach (var job in App.Services.ConversionQueueRuntime.Snapshot.Jobs)
        {
            _media.Add(ImportedMediaRow.FromJob(job));
        }
        App.Services.ConversionQueueRuntime.Queue.Changed += OnQueueChanged;
        App.Services.ConversionQueueRuntime.ProgressChanged += OnProgressChanged;
        Unloaded += OnUnloaded;
    }

    private void ApplyStrings()
    {
        var strings = App.Services.Localization;
        TitleTextBlock.Text = strings.GetString("Conversion.Title");
        AddFilesButton.Label = strings.GetString("Conversion.AddFiles");
        AddFilesButton.SetValue(Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty, AddFilesButton.Label);
        AddFolderButton.Label = strings.GetString("Conversion.AddFolder");
        AddFolderButton.SetValue(Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty, AddFolderButton.Label);
        StartButton.Label = strings.GetString("Conversion.Start");
        ClearButton.Label = strings.GetString("Conversion.Clear");
        ExitButton.Label = strings.GetString("Application.Exit");
        ExitButton.SetValue(Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty, ExitButton.Label);
        EmptyTitleTextBlock.Text = strings.GetString("Conversion.EmptyTitle");
        EmptyDescriptionTextBlock.Text = strings.GetString("Conversion.EmptyDescription");
    }

    private async void OnAddFilesClick(object sender, RoutedEventArgs args)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);
        var files = await picker.PickMultipleFilesAsync();
        await ProbeFilesAsync(files);
    }

    private async void OnStartClick(object sender, RoutedEventArgs args)
    {
        try
        {
            var settings = (await App.Services.SettingsStore.LoadAsync()).Settings;
            var resolution = App.Services.FfmpegToolResolver.Resolve(settings.FfmpegDirectory);
            if (!resolution.IsSuccess)
            {
                ShowStatus(App.Services.Localization.GetString("Conversion.ToolsNotFound"), InfoBarSeverity.Error);
                return;
            }

            var capabilities = await App.Services.FfmpegCapabilityService.GetAsync(resolution.Toolset!, forceRefresh: false);
            var guard = new FfmpegFeatureGuard();
            var requirements = new[]
            {
                new FfmpegFeatureRequirement("MP4 output", FfmpegFeatureKind.Muxer, "mp4"),
                new FfmpegFeatureRequirement("H.264 video encoding", FfmpegFeatureKind.Encoder, "libx264"),
                new FfmpegFeatureRequirement("AAC audio encoding", FfmpegFeatureKind.Encoder, "aac")
            };
            var unsupported = requirements
                .Select(requirement => guard.Check(capabilities, requirement))
                .FirstOrDefault(result => !result.IsSupported);
            if (unsupported is not null)
            {
                ShowStatus(string.Format(
                    App.Services.Localization.GetString("Conversion.UnsupportedFeature"),
                    unsupported.MissingCapability), InfoBarSeverity.Error);
                return;
            }

            var sources = _media.Where(row => row.Source is not null && row.JobId is null)
                .Select(row => row.Source!)
                .ToArray();
            if (sources.Length == 0)
            {
                ShowStatus(App.Services.Localization.GetString("Conversion.NoFilesToQueue"), InfoBarSeverity.Warning);
                return;
            }

            var jobs = App.Services.ConversionQueueRuntime.AddDefaultJobs(sources, settings);
            foreach (var job in jobs)
            {
                var index = _media.ToList().FindIndex(row => string.Equals(row.Path, job.InputPath, StringComparison.OrdinalIgnoreCase) && row.JobId is null);
                if (index >= 0)
                {
                    _media[index] = _media[index] with
                    {
                        JobId = job.Id,
                        JobStatus = MediaForge.Core.Jobs.ConversionJobStatus.Queued,
                        QueueStatus = QueueStatusText(MediaForge.Core.Jobs.ConversionJobStatus.Queued)
                    };
                }
            }
            await App.Services.ConversionQueueRuntime.StartAsync();
            ShowStatus(string.Format(App.Services.Localization.GetString("Conversion.QueuedCount"), jobs.Count), InfoBarSeverity.Success);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ShowStatus(error.Message, InfoBarSeverity.Error);
        }
    }

    private void OnClearClick(object sender, RoutedEventArgs args)
    {
        if (App.Services.ConversionQueueRuntime.Snapshot.Jobs.Any(job => job.Status == MediaForge.Core.Jobs.ConversionJobStatus.Running))
        {
            ShowStatus(App.Services.Localization.GetString("Conversion.CannotClearRunning"), InfoBarSeverity.Warning);
            return;
        }

        App.Services.ConversionQueueRuntime.Queue.Clear();
        _media.Clear();
    }

    private async void OnExitClick(object sender, RoutedEventArgs args) => await App.ExitAsync();

    private void OnQueueChanged(object? sender, MediaForge.Core.Jobs.ConversionQueueChangedEventArgs args)
    {
        App.DispatcherQueue.TryEnqueue(() =>
        {
            var statuses = args.Snapshot.Jobs.ToDictionary(job => job.Id);
            for (var index = 0; index < _media.Count; index++)
            {
                var row = _media[index];
                if (row.JobId is not { } jobId || !statuses.TryGetValue(jobId, out var job))
                {
                    continue;
                }
                _media[index] = row with { JobStatus = job.Status, QueueStatus = QueueStatusText(job.Status) };
            }
            UpdateOverallProgress();
        });
    }

    private void OnProgressChanged(object? sender, MediaForge.App.Services.ConversionProgressChangedEventArgs args)
    {
        App.DispatcherQueue.TryEnqueue(() =>
        {
            for (var index = 0; index < _media.Count; index++)
            {
                if (_media[index].JobId != args.Progress.JobId) continue;
                _media[index] = _media[index] with
                {
                    ProgressPercentage = args.Progress.Percentage ?? 0,
                    ProgressVisibility = Visibility.Visible,
                    ProgressText = FormatProgress(args.Progress, _media[index].Source?.Duration)
                };
                break;
            }
            UpdateOverallProgress();
        });
    }

    private static string FormatProgress(MediaForge.App.Services.ConversionProgressSnapshot progress, TimeSpan? sourceDuration)
    {
        var percent = progress.Percentage is { } value ? $"{value:F0}%" : App.Services.Localization.GetString("Conversion.ProgressUnknown");
        var speed = progress.Speed is { } multiplier ? $" · {multiplier:F2}x" : string.Empty;
        var elapsed = $" · {App.Services.Localization.GetString("Conversion.Elapsed")} {progress.Elapsed:g}";
        var remaining = progress.EstimatedRemaining is { } duration && sourceDuration is not null
            ? $" · {App.Services.Localization.GetString("Conversion.Remaining")} {duration:g}"
            : string.Empty;
        return string.Concat(percent, speed, elapsed, remaining);
    }

    private void UpdateOverallProgress()
    {
        var jobs = _media.Where(row => row.JobId is not null).ToArray();
        OverallProgressPanel.Visibility = jobs.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (jobs.Length == 0) return;

        var value = jobs.Average(row => row.JobStatus is { } status && IsTerminalStatus(status) ? 100 : row.ProgressPercentage);
        OverallProgressBar.Value = value;
        OverallProgressTextBlock.Text = string.Format(App.Services.Localization.GetString("Conversion.OverallProgress"),
            jobs.Count(row => row.JobStatus is { } status && IsTerminalStatus(status)), jobs.Length, value);
    }

    private static bool IsTerminalStatus(MediaForge.Core.Jobs.ConversionJobStatus status) => status is
        MediaForge.Core.Jobs.ConversionJobStatus.Succeeded or
        MediaForge.Core.Jobs.ConversionJobStatus.Failed or
        MediaForge.Core.Jobs.ConversionJobStatus.Skipped or
        MediaForge.Core.Jobs.ConversionJobStatus.Interrupted;

    internal static string QueueStatusText(MediaForge.Core.Jobs.ConversionJobStatus status) =>
        App.Services.Localization.GetString($"Conversion.Status.{status}");

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        App.Services.ConversionQueueRuntime.Queue.Changed -= OnQueueChanged;
        App.Services.ConversionQueueRuntime.ProgressChanged -= OnProgressChanged;
        Unloaded -= OnUnloaded;
    }

    private async void OnAddFolderClick(object sender, RoutedEventArgs args)
    {
        var settings = (await App.Services.SettingsStore.LoadAsync()).Settings;
        var dialog = new FolderImportDialog(settings.LastFolderImport) { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || dialog.Result is not { } import)
        {
            return;
        }

        try
        {
            var resolution = App.Services.FfmpegToolResolver.Resolve(settings.FfmpegDirectory);
            if (!resolution.IsSuccess)
            {
                AddError(import.Directory!, App.Services.Localization.GetString("Conversion.ToolsNotFound"));
                return;
            }

            var service = new FolderMediaImportService(new FfprobeMediaProbeService(
                resolution.Toolset!, App.Services.FfmpegProcessRunner));
            var files = await service.ImportFolderAsync(
                import.Directory!, import.IncludeSubdirectories,
                FileNameFilter.Create(import.FilterMode, import.FilterExpression), settings.MaxConcurrentJobs);
            foreach (var file in files)
            {
                Add(file.Path, file.Media, file.Error);
            }

            await App.Services.SettingsStore.SaveAsync(settings with { LastFolderImport = import });
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            AddError(import.Directory!, error.Message);
        }
    }

    private async void OnDrop(object sender, DragEventArgs args)
    {
        if (!args.DataView.Contains(StandardDataFormats.StorageItems)) return;
        var deferral = args.GetDeferral();
        try
        {
            var items = await args.DataView.GetStorageItemsAsync();
            await ProbeFilesAsync(items.OfType<StorageFile>());
        }
        finally { deferral.Complete(); }
    }

    private void OnDragOver(object sender, DragEventArgs args)
    {
        args.AcceptedOperation = args.DataView.Contains(StandardDataFormats.StorageItems)
            ? DataPackageOperation.Copy : DataPackageOperation.None;
    }

    private async Task ProbeFilesAsync(IEnumerable<StorageFile> files)
    {
        var settings = (await App.Services.SettingsStore.LoadAsync()).Settings;
        var resolution = App.Services.FfmpegToolResolver.Resolve(settings.FfmpegDirectory);
        if (!resolution.IsSuccess)
        {
            foreach (var file in files) AddError(file.Path, App.Services.Localization.GetString("Conversion.ToolsNotFound"));
            return;
        }

        var probe = new FfprobeMediaProbeService(resolution.Toolset!, App.Services.FfmpegProcessRunner);
        foreach (var file in files)
        {
            try { Add(file.Path, await probe.ProbeAsync(file.Path), null); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
            { AddError(file.Path, error.Message); }
        }
    }

    private void Add(string path, MediaForge.Core.Media.MediaSourceInfo? media, string? error)
    {
        if (_media.Any(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))) return;
        _media.Add(ImportedMediaRow.Create(path, media, error));
    }

    private void AddError(string path, string error) => Add(path, null, error);

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        ConversionStatusInfoBar.Message = message;
        ConversionStatusInfoBar.Severity = severity;
        ConversionStatusInfoBar.IsOpen = true;
    }
}

public sealed record ImportedMediaRow(
    string Path,
    string FileName,
    string Summary,
    MediaForge.Core.Media.MediaSourceInfo? Source,
    Guid? JobId,
    MediaForge.Core.Jobs.ConversionJobStatus? JobStatus,
    string? QueueStatus,
    double ProgressPercentage,
    Visibility ProgressVisibility,
    string? ProgressText)
{
    public static ImportedMediaRow Create(string path, MediaForge.Core.Media.MediaSourceInfo? media, string? error)
    {
        var summary = error ?? (media is null ? App.Services.Localization.GetString("Conversion.UnsupportedFile") : Format(media));
        return new ImportedMediaRow(path, System.IO.Path.GetFileName(path), summary, media, null, null, null, 0, Visibility.Collapsed, null);
    }

    public static ImportedMediaRow FromJob(MediaForge.Core.Jobs.ConversionJobSnapshot job) =>
        new(job.InputPath, System.IO.Path.GetFileName(job.InputPath),
            string.Format(App.Services.Localization.GetString("Conversion.Output"), job.OutputPath),
            null, job.Id, job.Status, ConversionPage.QueueStatusText(job.Status), 0, Visibility.Collapsed, null);

    private static string Format(MediaForge.Core.Media.MediaSourceInfo media)
    {
        var video = media.Streams.FirstOrDefault(stream => stream.Type == MediaForge.Core.Media.MediaStreamType.Video);
        var audio = media.Streams.FirstOrDefault(stream => stream.Type == MediaForge.Core.Media.MediaStreamType.Audio);
        var duration = media.Duration?.ToString("g") ?? App.Services.Localization.GetString("Conversion.UnknownDuration");
        var resolution = video?.Width is not null && video.Height is not null ? $"{video.Width}×{video.Height}" : null;
        return string.Join(" · ", new[] { duration, resolution, video?.Codec, video?.FrameRate, audio?.Codec, media.BitRate is null ? null : $"{media.BitRate / 1000} kb/s" }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
