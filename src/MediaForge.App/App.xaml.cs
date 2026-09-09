using MediaForge.App.Services;
using MediaForge.Core.Diagnostics;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MediaForge.App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// The main application window. Use <c>App.Window</c> from any class that needs
    /// the window reference (for dialogs, pickers, interop, etc.).
    /// </summary>
    public static Window Window { get; private set; } = null!;

    /// <summary>
    /// The UI thread dispatcher. Use <c>App.DispatcherQueue</c> to marshal calls
    /// to the UI thread. Fully qualified to avoid CS0104 ambiguity with
    /// <see cref="Windows.System.DispatcherQueue"/>.
    /// </summary>
    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    public static ApplicationServices Services { get; private set; } = null!;

    /// <summary>
    /// The native window handle (HWND). Use for file pickers,
    /// <c>DataTransferManager</c>, and any WinRT interop that requires
    /// <c>InitializeWithWindow</c>.
    /// </summary>
    public static nint WindowHandle =>
        WinRT.Interop.WindowNative.GetWindowHandle(Window);

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            Services = ApplicationServices.Create(AppContext.BaseDirectory);
            var cleanupResults = await Services.TemporaryOutputRecoveryService.RecoverAsync();
            Services.Logger.Log(new ApplicationLogEntry(
                DateTimeOffset.UtcNow,
                ApplicationLogLevel.Information,
                "ApplicationLaunched"));
            foreach (var cleanup in cleanupResults.Where(result => result.Deleted))
            {
                Services.Logger.Log(new ApplicationLogEntry(
                    DateTimeOffset.UtcNow,
                    ApplicationLogLevel.Information,
                    "RecoveredTemporaryOutput",
                    cleanup.TemporaryPath));
            }

            var mainWindow = new MainWindow();
            mainWindow.Initialize(Services.ApplicationPaths);
            Window = mainWindow;
            DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            Window.Activate();
        }
        catch (Exception error)
        {
            Services?.Logger.LogError("ApplicationLaunchFailed", error);
            throw;
        }
    }

    private static void OnUnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs args) =>
        Services?.Logger.LogError("UnhandledUiException", args.Exception);

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Services?.Logger.LogError("UnobservedTaskException", args.Exception);
        args.SetObserved();
    }
}
