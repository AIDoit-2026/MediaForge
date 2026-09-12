using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace MediaForge.App.Pages;

public sealed partial class SettingsPage : Page
{
    private const int SaveDebounceMilliseconds = 400;

    private ApplicationSettings _settings = ApplicationSettings.CreateDefault();
    private bool _loaded;
    private bool _saving;
    private bool _dirty;
    private readonly DispatcherTimer _saveTimer;

    public SettingsPage()
    {
        InitializeComponent();
        ApplyStrings();
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SaveDebounceMilliseconds) };
        _saveTimer.Tick += async (_, _) =>
        {
            _saveTimer.Stop();
            await SaveIfChangedAsync();
        };
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        Loaded -= OnLoaded;
        var loaded = await App.Services.SettingsStore.LoadAsync();
        _settings = loaded.Settings;

        LanguageComboBox.SelectionChanged -= OnSettingChanged;
        ThemeComboBox.SelectionChanged -= OnSettingChanged;
        ConflictPolicyComboBox.SelectionChanged -= OnSettingChanged;
        MaxConcurrencyNumberBox.ValueChanged -= OnNumberSettingChanged;
        FfmpegDirectoryTextBox.LostFocus -= OnSettingChanged;
        DefaultOutputDirectoryTextBox.LostFocus -= OnSettingChanged;

        LanguageComboBox.SelectedIndex = (int)_settings.Language;
        ThemeComboBox.SelectedIndex = (int)_settings.Theme;
        FfmpegDirectoryTextBox.Text = _settings.FfmpegDirectory ?? string.Empty;
        DefaultOutputDirectoryTextBox.Text = _settings.DefaultOutputDirectory ?? string.Empty;
        ConflictPolicyComboBox.SelectedIndex = (int)_settings.OutputConflictPolicy;
        MaxConcurrencyNumberBox.Value = _settings.MaxConcurrentJobs;

        LanguageComboBox.SelectionChanged += OnSettingChanged;
        ThemeComboBox.SelectionChanged += OnSettingChanged;
        ConflictPolicyComboBox.SelectionChanged += OnSettingChanged;
        MaxConcurrencyNumberBox.ValueChanged += OnNumberSettingChanged;
        FfmpegDirectoryTextBox.LostFocus += OnSettingChanged;
        DefaultOutputDirectoryTextBox.LostFocus += OnSettingChanged;

        if (loaded.RecoveredFromCorruption)
        {
            ShowStatus(Strings("Settings.CorruptRecovered"), InfoBarSeverity.Warning);
        }

        if (!App.Services.ApplicationPaths.CanPersist)
        {
            ShowStatus(Strings("Settings.CannotPersist"), InfoBarSeverity.Warning);
        }

        _loaded = true;
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        _saveTimer.Stop();
        if (_dirty)
        {
            _ = PersistAsync();
        }
    }

    private void OnSettingChanged(object sender, RoutedEventArgs args)
    {
        if (!_loaded) return;
        _dirty = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void OnNumberSettingChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_loaded) return;
        _dirty = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private async Task SaveIfChangedAsync()
    {
        if (!_loaded) return;
        var concurrency = (int)Math.Round(MaxConcurrencyNumberBox.Value);
        if (concurrency is < 1 or > 4)
        {
            ShowStatus(Strings("Settings.InvalidConcurrency"), InfoBarSeverity.Error);
            return;
        }

        var updated = _settings with
        {
            Language = (ApplicationLanguage)LanguageComboBox.SelectedIndex,
            Theme = (MediaForge.Core.Configuration.ApplicationTheme)ThemeComboBox.SelectedIndex,
            FfmpegDirectory = EmptyToNull(FfmpegDirectoryTextBox.Text),
            DefaultOutputDirectory = EmptyToNull(DefaultOutputDirectoryTextBox.Text),
            OutputConflictPolicy = (OutputConflictPolicy)ConflictPolicyComboBox.SelectedIndex,
            MaxConcurrentJobs = concurrency
        };
        if (updated == _settings) return;

        await PersistAsync(updated);
    }

    private async Task PersistAsync(ApplicationSettings? updated = null)
    {
        if (_saving) { _dirty = true; return; }
        _saving = true;
        try
        {
            var settings = updated ?? CollectSettings();
            if (settings is null) return;

            await App.Services.SettingsStore.SaveAsync(settings);
            _settings = settings;
            _dirty = false;
            if (!App.Services.ApplicationPaths.CanPersist) return;

            var languageChanged = App.Services.Localization.Language != settings.Language;
            App.Services.Localization.Apply(settings.Language);
            App.Services.Theme.Apply(settings.Theme, App.Window.Content as FrameworkElement);
            ShowFloatingTip(Strings("Settings.AutoSaved"));
            if (languageChanged)
            {
                (App.Window as MainWindow)?.RefreshShell();
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private ApplicationSettings? CollectSettings()
    {
        var concurrency = (int)Math.Round(MaxConcurrencyNumberBox.Value);
        if (concurrency is < 1 or > 4)
        {
            ShowStatus(Strings("Settings.InvalidConcurrency"), InfoBarSeverity.Error);
            return null;
        }
        return _settings with
        {
            Language = (ApplicationLanguage)LanguageComboBox.SelectedIndex,
            Theme = (MediaForge.Core.Configuration.ApplicationTheme)ThemeComboBox.SelectedIndex,
            FfmpegDirectory = EmptyToNull(FfmpegDirectoryTextBox.Text),
            DefaultOutputDirectory = EmptyToNull(DefaultOutputDirectoryTextBox.Text),
            OutputConflictPolicy = (OutputConflictPolicy)ConflictPolicyComboBox.SelectedIndex,
            MaxConcurrentJobs = concurrency
        };
    }

    private async void OnFfmpegDirectoryBrowseClick(object sender, RoutedEventArgs args) =>
        await PickDirectoryAsync(FfmpegDirectoryTextBox);

    private async void OnDefaultOutputDirectoryBrowseClick(object sender, RoutedEventArgs args) =>
        await PickDirectoryAsync(DefaultOutputDirectoryTextBox);

    private async Task PickDirectoryAsync(TextBox target)
    {
        var picker = new FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            target.Text = folder.Path;
            await SaveIfChangedAsync();
        }
    }

    private void ShowFloatingTip(string message)
    {
        AutoSaveTipTextBlock.Text = message;
        AutoSaveTip.IsOpen = true;
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
