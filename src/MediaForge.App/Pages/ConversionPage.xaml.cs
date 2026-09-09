using System.Collections.ObjectModel;
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
}

public sealed record ImportedMediaRow(string Path, string FileName, string Summary)
{
    public static ImportedMediaRow Create(string path, MediaForge.Core.Media.MediaSourceInfo? media, string? error)
    {
        var summary = error ?? (media is null ? App.Services.Localization.GetString("Conversion.UnsupportedFile") : Format(media));
        return new ImportedMediaRow(path, System.IO.Path.GetFileName(path), summary);
    }

    private static string Format(MediaForge.Core.Media.MediaSourceInfo media)
    {
        var video = media.Streams.FirstOrDefault(stream => stream.Type == MediaForge.Core.Media.MediaStreamType.Video);
        var audio = media.Streams.FirstOrDefault(stream => stream.Type == MediaForge.Core.Media.MediaStreamType.Audio);
        var duration = media.Duration?.ToString("g") ?? App.Services.Localization.GetString("Conversion.UnknownDuration");
        var resolution = video?.Width is not null && video.Height is not null ? $"{video.Width}×{video.Height}" : null;
        return string.Join(" · ", new[] { duration, resolution, video?.Codec, video?.FrameRate, audio?.Codec, media.BitRate is null ? null : $"{media.BitRate / 1000} kb/s" }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
