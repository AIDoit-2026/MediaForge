using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaForge.App.Pages;

public sealed partial class PresetsPage : Page
{
    public PresetsPage()
    {
        InitializeComponent();
        TitleTextBlock.Text = App.Services.Localization.GetString("PresetsTitle.Text");
        DescriptionTextBlock.Text = App.Services.Localization.GetString("PresetsDescription.Text");
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        Loaded -= OnLoaded;
        RefreshItems();
    }

    private void RefreshItems() =>
        PresetListView.ItemsSource = BuiltInPresetCatalog.All
            .Select(preset => new PresetItem(
                preset.Id,
                preset.Name,
                Describe(preset.Parameters),
                preset.Id == App.Services.ConversionQueueRuntime.SelectedPreset?.Id))
            .ToArray();

    private void OnUsePresetClick(object sender, RoutedEventArgs args)
    {
        if (sender is FrameworkElement { DataContext: PresetItem item } && BuiltInPresetCatalog.Find(item.Id) is { } preset)
        {
            App.Services.ConversionQueueRuntime.SetSelectedPreset(preset);
            RefreshItems();
            UsePresetConfirmationTeachingTip.Content = string.Format(
                App.Services.Localization.GetString("Presets.Selected"), preset.Name);
            UsePresetConfirmationTeachingTip.IsOpen = true;
        }
    }

    private static string Describe(ConversionParameterSnapshot parameters) =>
        string.Join(
            " · ",
            parameters.OutputContainer.ToUpperInvariant(),
            parameters.Values.TryGetValue("videoEncoder", out var videoEncoder) ? videoEncoder : App.Services.Localization.GetString("Preset.NoVideo"),
            parameters.Values.TryGetValue("audioEncoder", out var audioEncoder) ? audioEncoder : App.Services.Localization.GetString("Preset.NoAudio"));
}

public sealed record PresetItem(Guid Id, string Name, string Description, bool IsSelected)
{
    public string SelectedText => App.Services.Localization.GetString("Presets.CurrentSelection");
    public string UseButtonText => App.Services.Localization.GetString("Presets.UseButton");
    public Visibility SelectedVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;
}
