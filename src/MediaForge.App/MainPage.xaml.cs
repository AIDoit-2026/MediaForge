using MediaForge.App.Pages;
using MediaForge.App.ViewModels;
using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml.Controls;

namespace MediaForge.App;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = new MainPageViewModel();
        InitializeComponent();
        ContentFrame.Navigate(typeof(ConversionPage));
    }

    public void Initialize(IApplicationPaths applicationPaths) =>
        ViewModel.Initialize(applicationPaths);

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
