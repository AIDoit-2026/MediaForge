using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace MediaForge.App.Pages;

public sealed partial class SettingsPage : Page
{
    private ApplicationSettings _settings = ApplicationSettings.CreateDefault();

    public SettingsPage()
    {
        InitializeComponent();
        ApplyStrings();
        Loaded += OnLoaded;
    }

    private void ApplyStrings()
    {
        var strings = App.Services.Localization;
        TitleTextBlock.Text = strings.GetString("SettingsTitle.Text");
        DescriptionTextBlock.Text = strings.GetString("SettingsDescription.Text");
        LanguageLabel.Text = strings.GetString("LanguageLabel.Text");
        SystemLanguageOption.Content = strings.GetString("SystemLanguageOption.Content");
        ChineseLanguageOption.Content = strings.GetString("ChineseLanguageOption.Content");
        EnglishLanguageOption.Content = strings.GetString("EnglishLanguageOption.Content");
        ThemeLabel.Text = strings.GetString("ThemeLabel.Text");
        SystemThemeOption.Content = strings.GetString("SystemThemeOption.Content");
        LightThemeOption.Content = strings.GetString("LightThemeOption.Content");
        DarkThemeOption.Content = strings.GetString("DarkThemeOption.Content");
        FfmpegDirectoryLabel.Text = strings.GetString("FfmpegDirectoryLabel.Text");
        FfmpegDirectoryTextBox.PlaceholderText = strings.GetString("FfmpegDirectoryTextBox.PlaceholderText");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(FfmpegDirectoryTextBox, strings.GetString("FfmpegDirectoryTextBox.[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name"));
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(FfmpegDirectoryBrowseButton, strings.GetString("FfmpegDirectoryBrowseButton.[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name"));
        DefaultOutputDirectoryLabel.Text = strings.GetString("DefaultOutputDirectoryLabel.Text");
        DefaultOutputDirectoryTextBox.PlaceholderText = strings.GetString("DefaultOutputDirectoryTextBox.PlaceholderText");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(DefaultOutputDirectoryTextBox, strings.GetString("DefaultOutputDirectoryTextBox.[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name"));
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(DefaultOutputDirectoryBrowseButton, strings.GetString("DefaultOutputDirectoryBrowseButton.[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name"));
        ConflictPolicyLabel.Text = strings.GetString("ConflictPolicyLabel.Text");
        SkipConflictOption.Content = strings.GetString("SkipConflictOption.Content");
        OverwriteConflictOption.Content = strings.GetString("OverwriteConflictOption.Content");
        RenameConflictOption.Content = strings.GetString("RenameConflictOption.Content");
        MaxConcurrencyLabel.Text = strings.GetString("MaxConcurrencyLabel.Text");
        SaveButton.Content = strings.GetString("SaveButton.Content");
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
            ShowStatus(Strings("Settings.CorruptRecovered"), InfoBarSeverity.Warning);
        }

        if (!App.Services.ApplicationPaths.CanPersist)
        {
            SaveButton.IsEnabled = false;
            ShowStatus(Strings("Settings.CannotPersist"), InfoBarSeverity.Warning);
        }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs args)
    {
        if (MaxConcurrencyNumberBox.Value is < 1 or > 4)
        {
            ShowStatus(Strings("Settings.InvalidConcurrency"), InfoBarSeverity.Error);
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
        App.Services.Localization.Apply(_settings.Language);
        App.Services.Theme.Apply(_settings.Theme, App.Window.Content as FrameworkElement);
        ShowStatus(Strings("Settings.Saved"), InfoBarSeverity.Success);
        (App.Window as MainWindow)?.RefreshShell();
    }

    private async void OnFfmpegDirectoryBrowseClick(object sender, RoutedEventArgs args) =>
        await PickDirectoryAsync(FfmpegDirectoryTextBox);

    private async void OnDefaultOutputDirectoryBrowseClick(object sender, RoutedEventArgs args) =>
        await PickDirectoryAsync(DefaultOutputDirectoryTextBox);

    private static async Task PickDirectoryAsync(TextBox target)
    {
        var picker = new FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            target.Text = folder.Path;
        }
    }

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = severity;
        StatusInfoBar.IsOpen = true;
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Strings(string key) => App.Services.Localization.GetString(key);
}
