using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaForge.App.Pages;

public sealed partial class SettingsPage : Page
{
    private ApplicationSettings _settings = ApplicationSettings.CreateDefault();

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        Loaded -= OnLoaded;
        var loaded = await App.Services.SettingsStore.LoadAsync();
        _settings = loaded.Settings;

        LanguageComboBox.SelectedIndex = (int)_settings.Language;
        ThemeComboBox.SelectedIndex = (int)_settings.Theme;
        FfmpegDirectoryTextBox.Text = _settings.FfmpegDirectory ?? string.Empty;
        DefaultOutputDirectoryTextBox.Text = _settings.DefaultOutputDirectory ?? string.Empty;
        ConflictPolicyComboBox.SelectedIndex = (int)_settings.OutputConflictPolicy;
        MaxConcurrencyNumberBox.Value = _settings.MaxConcurrentJobs;

        if (loaded.RecoveredFromCorruption)
        {
            ShowStatus("检测到损坏的设置文件，已备份并恢复为默认设置。", InfoBarSeverity.Warning);
        }

        if (!App.Services.ApplicationPaths.CanPersist)
        {
            SaveButton.IsEnabled = false;
            ShowStatus("当前临时会话无法保存设置。", InfoBarSeverity.Warning);
        }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs args)
    {
        if (MaxConcurrencyNumberBox.Value is < 1 or > 4)
        {
            ShowStatus("最大并行数必须在 1 到 4 之间。", InfoBarSeverity.Error);
            return;
        }

        _settings = _settings with
        {
            Language = (ApplicationLanguage)LanguageComboBox.SelectedIndex,
            Theme = (MediaForge.Core.Configuration.ApplicationTheme)ThemeComboBox.SelectedIndex,
            FfmpegDirectory = EmptyToNull(FfmpegDirectoryTextBox.Text),
            DefaultOutputDirectory = EmptyToNull(DefaultOutputDirectoryTextBox.Text),
            OutputConflictPolicy = (OutputConflictPolicy)ConflictPolicyComboBox.SelectedIndex,
            MaxConcurrentJobs = (int)MaxConcurrencyNumberBox.Value
        };

        await App.Services.SettingsStore.SaveAsync(_settings);
        ShowStatus("设置已保存。", InfoBarSeverity.Success);
    }

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = severity;
        StatusInfoBar.IsOpen = true;
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
