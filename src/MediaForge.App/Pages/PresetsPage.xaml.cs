using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml.Controls;

namespace MediaForge.App.Pages;

public sealed partial class PresetsPage : Page
{
    public PresetsPage()
    {
        InitializeComponent();
        TitleTextBlock.Text = App.Services.Localization.GetString("PresetsTitle.Text");
        DescriptionTextBlock.Text = App.Services.Localization.GetString("PresetsDescription.Text");
        PresetListView.ItemsSource = BuiltInPresetCatalog.All
            .Select(preset => new PresetItem(
                preset.Name,
                Describe(preset.Parameters)))
            .ToArray();
    }

    private static string Describe(ConversionParameterSnapshot parameters) =>
        string.Join(
            " · ",
            parameters.OutputContainer.ToUpperInvariant(),
            parameters.Values.TryGetValue("videoEncoder", out var videoEncoder) ? videoEncoder : App.Services.Localization.GetString("Preset.NoVideo"),
            parameters.Values.TryGetValue("audioEncoder", out var audioEncoder) ? audioEncoder : App.Services.Localization.GetString("Preset.NoAudio"));
}

public sealed record PresetItem(string Name, string Description);
