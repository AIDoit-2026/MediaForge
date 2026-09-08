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

        AppWindow.SetIcon("Assets/AppIcon.ico");

    }

    public void Initialize(IApplicationPaths applicationPaths)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
        RootFrame.Content = new MainPage(applicationPaths);
    }
}
