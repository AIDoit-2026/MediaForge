using System.Runtime.InteropServices;
using MediaForge.App.Services;
using MediaForge.Core.Configuration;
using Microsoft.UI.Xaml;
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
    private const int GwlpWndProc = -4;
    private const uint WmClose = 0x0010;
    private const int SwHide = 0;
    private const int SwShow = 5;
    private static readonly SizeInt32 MinimumWindowSize = new(720, 520);
    private readonly nint _windowHandle;
    private readonly WindowProc _windowProc;
    private bool _enforcingMinimumSize;
    private bool _exitRequested;
    private nint _previousWindowProc;
    private TrayIconService? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();

        _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _windowProc = WindowProcedure;
        _previousWindowProc = SetWindowLongPtr(
            _windowHandle,
            GwlpWndProc,
            Marshal.GetFunctionPointerForDelegate(_windowProc));
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
        _trayIcon ??= new TrayIconService(
            _windowHandle,
            Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"),
            ShowFromTray,
            App.ExitAsync);
        if (!_trayIcon.IsRegistered)
        {
            App.Services.Logger.Log(new MediaForge.Core.Diagnostics.ApplicationLogEntry(
                DateTimeOffset.UtcNow,
                MediaForge.Core.Diagnostics.ApplicationLogLevel.Warning,
                "TrayIconRegistrationFailed",
                _trayIcon.RegistrationError.ToString()));
        }
        UpdateTrayStrings();
    }

    public void RefreshShell()
    {
        var currentPage = (RootFrame.Content as MainPage)?.CurrentPageType;
        var mainPage = new MainPage(currentPage);
        mainPage.Initialize(App.Services.ApplicationPaths);
        RootFrame.Content = mainPage;
        App.Services.Theme.Apply(App.Services.Theme.Theme, Content as FrameworkElement);
        UpdateTrayStrings();
    }

    internal void PrepareForExit() => _exitRequested = true;

    internal void DisposeForExit()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
        if (_previousWindowProc != 0)
        {
            SetWindowLongPtr(_windowHandle, GwlpWndProc, _previousWindowProc);
            _previousWindowProc = 0;
        }
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

    private void OnAppWindowChanged(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
    {
        if (!args.DidSizeChange || _enforcingMinimumSize)
        {
            return;
        }

        var size = sender.Size;
        if (size.Width >= MinimumWindowSize.Width && size.Height >= MinimumWindowSize.Height)
        {
            return;
        }

        _enforcingMinimumSize = true;
        try
        {
            sender.Resize(new SizeInt32(
                Math.Max(size.Width, MinimumWindowSize.Width),
                Math.Max(size.Height, MinimumWindowSize.Height)));
        }
        finally
        {
            _enforcingMinimumSize = false;
        }
    }

    private void UpdateTrayStrings()
    {
        _trayIcon?.UpdateStrings(
            App.Services.Localization.GetString("Application.Tray.Open"),
            App.Services.Localization.GetString("Application.Tray.Exit"),
            App.Services.Localization.GetString("AppTitle"));
    }

    private void ShowFromTray()
    {
        ShowWindow(_windowHandle, SwShow);
        SetForegroundWindow(_windowHandle);
    }

    private nint WindowProcedure(nint hWnd, uint message, nint wParam, nint lParam)
    {
        if (_trayIcon?.TryHandleMessage(message, lParam) == true)
        {
            return 0;
        }

        if (message == WmClose && !_exitRequested)
        {
            ShowWindow(hWnd, SwHide);
            return 0;
        }

        return CallWindowProc(_previousWindowProc, hWnd, message, wParam, lParam);
    }

    private delegate nint WindowProc(nint hWnd, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int index, nint newLong);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern nint CallWindowProc(nint previousWindowProc, nint hWnd, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint hWnd, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hWnd);
}
