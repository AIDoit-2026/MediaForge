using MediaForge.Core.Configuration;
using MediaForge.Core.Diagnostics;
using MediaForge.Core.Ffmpeg;
using MediaForge.Infrastructure.Configuration;
using MediaForge.Infrastructure.Diagnostics;
using MediaForge.Infrastructure.Ffmpeg;

namespace MediaForge.App.Services;

public sealed class ApplicationServices : IDisposable
{
    private ApplicationServices(
        IApplicationPaths applicationPaths,
        IApplicationLogger logger,
        IApplicationSettingsStore settingsStore,
        IQueueStore queueStore,
        IUserPresetStore userPresetStore,
        IFfmpegToolResolver ffmpegToolResolver,
        IFfmpegVersionReader ffmpegVersionReader,
        IFfmpegCapabilityService ffmpegCapabilityService,
        IHardwareEncoderProbe hardwareEncoderProbe,
        IFfmpegProcessRunner ffmpegProcessRunner)
    {
        ApplicationPaths = applicationPaths;
        Logger = logger;
        SettingsStore = settingsStore;
        QueueStore = queueStore;
        UserPresetStore = userPresetStore;
        FfmpegToolResolver = ffmpegToolResolver;
        FfmpegVersionReader = ffmpegVersionReader;
        FfmpegCapabilityService = ffmpegCapabilityService;
        HardwareEncoderProbe = hardwareEncoderProbe;
        FfmpegProcessRunner = ffmpegProcessRunner;
    }

    public IApplicationPaths ApplicationPaths { get; }

    public IApplicationLogger Logger { get; }

    public IApplicationSettingsStore SettingsStore { get; }

    public IQueueStore QueueStore { get; }

    public IUserPresetStore UserPresetStore { get; }

    public IFfmpegToolResolver FfmpegToolResolver { get; }

    public IFfmpegVersionReader FfmpegVersionReader { get; }

    public IFfmpegCapabilityService FfmpegCapabilityService { get; }

    public IHardwareEncoderProbe HardwareEncoderProbe { get; }

    public IFfmpegProcessRunner FfmpegProcessRunner { get; }

    public static ApplicationServices Create(string baseDirectory)
    {
        var applicationPaths = PortableApplicationPaths.Create(baseDirectory);
        var logger = new JsonLineApplicationLogger(applicationPaths);
        var processRunner = new FfmpegProcessRunner();
        return new ApplicationServices(
            applicationPaths,
            logger,
            new ApplicationSettingsStore(applicationPaths, new AtomicJsonFileStore()),
            new QueueStore(applicationPaths, new AtomicJsonFileStore()),
            new UserPresetStore(applicationPaths, new AtomicJsonFileStore()),
            new FfmpegToolResolver(applicationPaths.BaseDirectory),
            new FfmpegVersionReader(),
            new FfmpegCapabilityService(applicationPaths, new AtomicJsonFileStore(), processRunner),
            new HardwareEncoderProbe(),
            processRunner);
    }

    public void Dispose()
    {
        if (Logger is IDisposable disposableLogger)
        {
            disposableLogger.Dispose();
        }
    }
}
