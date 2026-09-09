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
        TemporarySessionInfoBar.Title = "当前为临时会话";
        TemporarySessionInfoBar.Message = "无法写入 config 文件夹。本次将使用默认参数，设置、队列和日志不会保存。";
        ConversionNavigationItem.Content = "转换";
        PresetsNavigationItem.Content = "预设";
        if (RootNavigation.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = "设置";
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
