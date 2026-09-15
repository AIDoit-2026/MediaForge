using System.Text.Json;
using System.Text.Json.Serialization;
using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaForge.App.Pages;

public sealed partial class PresetsPage : Page
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PresetsPage()
    {
        InitializeComponent();
        var strings = App.Services.Localization;
        TitleTextBlock.Text = strings.GetString("PresetsTitle.Text");
        DescriptionTextBlock.Text = strings.GetString("PresetsDescription.Text");
        AddButton.Content = strings.GetString("Presets.Add");
        ReloadButton.Content = strings.GetString("Presets.Reload");
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs args) { Loaded -= OnLoaded; await RefreshItemsAsync(); }
    private async void OnReloadClick(object sender, RoutedEventArgs args) => await RefreshItemsAsync();

    private async void OnAddClick(object sender, RoutedEventArgs args)
    {
        var preset = new PresetDocument(PresetDocument.CurrentSchemaVersion, Guid.NewGuid(), "New preset", ConversionParameterSnapshot.CreateDefault());
        if (await EditPresetAsync(preset) is { } saved) { await App.Services.UserPresetStore.SaveAsync(saved); await RefreshItemsAsync(); }
    }

    private async Task RefreshItemsAsync()
    {
        var selected = App.Services.ConversionQueueRuntime.SelectedPreset?.Id;
        var presets = await App.Services.UserPresetStore.LoadAsync();
        PresetListView.ItemsSource = presets.Select(p => new PresetItem(p.Id, p.Name, Describe(p.Parameters), p, p.Id == selected)).ToArray();
    }

    private async void OnUsePresetClick(object sender, RoutedEventArgs args)
    {
        if (sender is FrameworkElement { DataContext: PresetItem item })
        {
            App.Services.ConversionQueueRuntime.SetSelectedPreset(item.Preset);
            await RefreshItemsAsync();
            UsePresetConfirmationTeachingTip.Content = string.Format(App.Services.Localization.GetString("Presets.Selected"), item.Name);
            UsePresetConfirmationTeachingTip.IsOpen = true;
        }
    }

    private async void OnEditPresetClick(object sender, RoutedEventArgs args)
    {
        if (sender is FrameworkElement { DataContext: PresetItem item } && await EditPresetAsync(item.Preset) is { } edited)
        {
            await App.Services.UserPresetStore.SaveAsync(edited);
            if (App.Services.ConversionQueueRuntime.SelectedPreset?.Id == edited.Id) App.Services.ConversionQueueRuntime.SetSelectedPreset(edited);
            await RefreshItemsAsync();
        }
    }

    private async void OnDeletePresetClick(object sender, RoutedEventArgs args)
    {
        if (sender is not FrameworkElement { DataContext: PresetItem item }) return;
        await App.Services.UserPresetStore.DeleteAsync(item.Id);
        if (App.Services.ConversionQueueRuntime.SelectedPreset?.Id == item.Id) App.Services.ConversionQueueRuntime.SetSelectedPreset(null);
        await RefreshItemsAsync();
    }

    private async Task<PresetDocument?> EditPresetAsync(PresetDocument preset)
    {
        var editor = new TextBox
        {
            Text = JsonSerializer.Serialize(preset, JsonOptions), AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), MinWidth = 620, MinHeight = 360,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var dialog = new ContentDialog
        {
            Title = App.Services.Localization.GetString("Presets.EditTitle"), Content = editor,
            PrimaryButtonText = App.Services.Localization.GetString("Presets.Save"),
            CloseButtonText = App.Services.Localization.GetString("FolderImport.Cancel"), XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return null;
        try
        {
            var result = JsonSerializer.Deserialize<PresetDocument>(editor.Text, JsonOptions);
            if (result is null || result.Id == Guid.Empty || string.IsNullOrWhiteSpace(result.Name)) throw new JsonException();
            return PresetDocumentMigration.MigrateToCurrent(result) with { Id = preset.Id };
        }
        catch (Exception error) when (error is JsonException or NotSupportedException)
        {
            UsePresetConfirmationTeachingTip.Content = App.Services.Localization.GetString("Presets.InvalidJson");
            UsePresetConfirmationTeachingTip.IsOpen = true;
            return null;
        }
    }

    private static string Describe(ConversionParameterSnapshot p) => string.Join(" / ", p.OutputContainer.ToUpperInvariant(),
        p.Values.TryGetValue("videoEncoder", out var video) ? video : App.Services.Localization.GetString("Preset.NoVideo"),
        p.Values.TryGetValue("audioEncoder", out var audio) ? audio : App.Services.Localization.GetString("Preset.NoAudio"));
}

public sealed record PresetItem(Guid Id, string Name, string Description, PresetDocument Preset, bool IsSelected)
{
    public string SelectedText => App.Services.Localization.GetString("Presets.CurrentSelection");
    public string UseButtonText => App.Services.Localization.GetString("Presets.UseButton");
    public string EditButtonText => App.Services.Localization.GetString("Presets.Edit");
    public string DeleteButtonText => App.Services.Localization.GetString("Presets.Delete");
    public Visibility SelectedVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;
}
