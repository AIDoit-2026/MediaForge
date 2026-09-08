using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml.Controls;

namespace MediaForge.App.Pages;

public sealed partial class PresetsPage : Page
{
    public PresetsPage()
    {
        InitializeComponent();
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
            parameters.Values.TryGetValue("videoEncoder", out var videoEncoder) ? videoEncoder : "无视频",
            parameters.Values.TryGetValue("audioEncoder", out var audioEncoder) ? audioEncoder : "无音频");
}

public sealed record PresetItem(string Name, string Description);
