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
        var name = new TextBox { Header = "Name", Text = preset.Name };
        var container = new TextBox { Header = "Output container", Text = preset.Parameters.OutputContainer };
        var fields = new Dictionary<string, TextBox>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in new[] { "videoMode", "videoEncoder", "audioEncoder", "resolution", "qualityMode", "crf", "audioBitrateKbps" })
        {
            fields[key] = new TextBox { Header = key, Text = preset.Parameters.Values.TryGetValue(key, out var value) ? value : string.Empty };
        }
        var form = new StackPanel { Spacing = 8, Children = { name, container } };
        foreach (var field in fields.Values) form.Children.Add(field);
        var editor = new TextBox
        {
            Text = JsonSerializer.Serialize(preset, JsonOptions), AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), MinWidth = 620, MinHeight = 360,
            Visibility = Visibility.Collapsed
        };
        editor.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        editor.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        var modeButton = new Button { Content = "Edit JSON" };
        modeButton.Click += (_, _) =>
        {
            var jsonMode = editor.Visibility == Visibility.Visible;
            editor.Visibility = jsonMode ? Visibility.Collapsed : Visibility.Visible;
            form.Visibility = jsonMode ? Visibility.Visible : Visibility.Collapsed;
            modeButton.Content = jsonMode ? "Edit JSON" : "Edit fields";
        };
        var content = new StackPanel { Spacing = 8, Children = { modeButton, form, editor } };
        var dialog = new ContentDialog
        {
            Title = App.Services.Localization.GetString("Presets.EditTitle"), Content = content,
            PrimaryButtonText = App.Services.Localization.GetString("Presets.Save"),
            CloseButtonText = App.Services.Localization.GetString("FolderImport.Cancel"), XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return null;
        try
        {
            PresetDocument? result;
            if (editor.Visibility == Visibility.Visible)
            {
                result = JsonSerializer.Deserialize<PresetDocument>(editor.Text, JsonOptions);
            }
            else
            {
                var values = fields.Where(pair => !string.IsNullOrWhiteSpace(pair.Value.Text))
                    .ToDictionary(pair => pair.Key, pair => pair.Value.Text.Trim(), StringComparer.OrdinalIgnoreCase);
                result = preset with
                {
                    Name = name.Text.Trim(),
                    Parameters = new ConversionParameterSnapshot(container.Text.Trim(), values)
                };
            }
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
