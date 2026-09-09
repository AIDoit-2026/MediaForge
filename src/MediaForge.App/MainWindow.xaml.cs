using Microsoft.UI.Xaml;
using MediaForge.Core.Configuration;
using Windows.Graphics;

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
    private static readonly SizeInt32 MinimumWindowSize = new(720, 520);
    private bool _enforcingMinimumSize;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        AppWindow.Resize(MinimumWindowSize);
        AppWindow.Changed += OnAppWindowChanged;
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

    public void ShowStartupError(string message)
    {
        RootFrame.Content = new Microsoft.UI.Xaml.Controls.TextBlock
        {
            Margin = new Thickness(24),
            Text = $"MediaForge could not finish loading.\n\n{message}\n\nSee config\\logs\\application.ndjson for details.",
            TextWrapping = TextWrapping.Wrap
        };
    }

    private void OnAppWindowChanged(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
    {
        if (!args.DidSizeChange || _enforcingMinimumSize) return;

        var size = sender.Size;
        if (size.Width >= MinimumWindowSize.Width && size.Height >= MinimumWindowSize.Height) return;

        _enforcingMinimumSize = true;
        try
        {
            sender.Resize(new SizeInt32(
                Math.Max(size.Width, MinimumWindowSize.Width),
                Math.Max(size.Height, MinimumWindowSize.Height)));
        }
        finally { _enforcingMinimumSize = false; }
    }
}
