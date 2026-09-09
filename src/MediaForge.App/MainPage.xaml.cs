using MediaForge.App.Pages;
using MediaForge.App.ViewModels;
using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml.Controls;

namespace MediaForge.App;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; }

    public Type? CurrentPageType => ContentFrame.CurrentSourcePageType;

    public MainPage(Type? initialPage = null)
    {
        ViewModel = new MainPageViewModel();
        InitializeComponent();
        ApplyStrings();
        var page = initialPage ?? typeof(ConversionPage);
        ContentFrame.Navigate(page);
        RootNavigation.SelectedItem = page == typeof(PresetsPage) ? PresetsNavigationItem : ConversionNavigationItem;
    }

    public void Initialize(IApplicationPaths applicationPaths) =>
        ViewModel.Initialize(applicationPaths);

    private void ApplyStrings()
    {
        var strings = App.Services.Localization;
        TemporarySessionInfoBar.Title = strings.GetString("TemporarySession.Title");
        TemporarySessionInfoBar.Message = strings.GetString("TemporarySession.Message");
        ConversionNavigationItem.Content = strings.GetString("Navigation.Conversion");
        PresetsNavigationItem.Content = strings.GetString("Navigation.Presets");
        if (RootNavigation.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = strings.GetString("Navigation.Settings");
        }
    }

    private void OnNavigationSelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (ContentFrame is null)
        {
            return;
        }

        var pageType = args.IsSettingsSelected
            ? typeof(SettingsPage)
            : (args.SelectedItemContainer?.Tag as string) switch
            {
                "presets" => typeof(PresetsPage),
                _ => typeof(ConversionPage)
            };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
