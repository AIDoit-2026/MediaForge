using Microsoft.UI.Xaml;
using MediaForge.Core.Configuration;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MediaForge.App;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
    }

    public void Initialize(IApplicationPaths applicationPaths)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
        var mainPage = new MainPage();
        mainPage.Initialize(applicationPaths);
        RootFrame.Content = mainPage;
    }

    public void RefreshShell()
    {
        var currentPage = (RootFrame.Content as MainPage)?.CurrentPageType;
        var mainPage = new MainPage(currentPage);
        mainPage.Initialize(App.Services.ApplicationPaths);
        RootFrame.Content = mainPage;
        App.Services.Theme.Apply(App.Services.Theme.Theme, Content as FrameworkElement);
    }
}
