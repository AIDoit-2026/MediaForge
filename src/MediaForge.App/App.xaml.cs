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
    private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(5);
    private static readonly object ShutdownGate = new();
    private static Task? _shutdownTask;
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

    internal static Task ShutdownAsync()
    {
        lock (ShutdownGate)
        {
            return _shutdownTask ??= ShutdownCoreAsync();
        }
    }

    internal static async Task ExitAsync()
    {
        (Window as MainWindow)?.PrepareForExit();
        try
        {
            var shutdown = ExitCoreAsync();
            await shutdown.WaitAsync(ExitTimeout);
        }
        catch (TimeoutException)
        {
            Services?.Logger.Log(new ApplicationLogEntry(
                DateTimeOffset.UtcNow,
                ApplicationLogLevel.Warning,
                "ApplicationExitTimedOut"));
        }
        catch (Exception error)
        {
            Services?.Logger.LogError("ApplicationExitFailed", error, error.ToString());
        }
        finally
        {
            Application.Current.Exit();
        }
    }

    private static async Task ExitCoreAsync()
    {
        if (Window is MainWindow mainWindow)
        {
            await mainWindow.SaveWindowSettingsAsync();
        }
        await ShutdownAsync();
    }

    private static async Task ShutdownCoreAsync()
    {
        if (Services is null)
        {
            return;
        }

        try
        {
            await Services.ConversionQueueRuntime.StopAndPersistAsync();
            await Services.TemporaryOutputRecoveryService.RecoverAsync();
            Services.Logger.Log(new ApplicationLogEntry(
                DateTimeOffset.UtcNow,
                ApplicationLogLevel.Information,
                "ApplicationExited"));
        }
        catch (Exception error)
        {
            Services.Logger.LogError("ApplicationShutdownFailed", error, error.ToString());
        }
        finally
        {
            (Window as MainWindow)?.DisposeForExit();
            Services.Dispose();
        }
    }

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
            await Services.UserPresetStore.EnsureBuiltInPresetsAsync();
            await Services.ConversionQueueRuntime.RestoreAsync();
            var mainWindow = new MainWindow();
            Window = mainWindow;
            DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            Window.Activate();

            _ = InitializeWindowAsync(mainWindow);
        }
        catch (Exception error)
        {
            Services?.Logger.LogError("ApplicationLaunchFailed", error, error.ToString());
            throw;
        }
    }

    private static async Task InitializeWindowAsync(MainWindow mainWindow)
    {
        try
        {
            var settings = await Services.SettingsStore.LoadAsync();
            Services.Localization.Apply(settings.Settings.Language);
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

            mainWindow.Initialize(Services.ApplicationPaths);
            mainWindow.ApplyWindowSettings(settings.Settings);
            Services.Theme.Apply(settings.Settings.Theme, mainWindow.Content as FrameworkElement);
        }
        catch (Exception error)
        {
            Services.Logger.LogError("ApplicationInitializationFailed", error, error.ToString());
            mainWindow.ShowStartupError(error.Message);
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
