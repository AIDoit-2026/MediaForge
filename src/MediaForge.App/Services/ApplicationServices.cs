using MediaForge.Core.Configuration;
using MediaForge.Core.Conversion;
using MediaForge.Core.Diagnostics;
using MediaForge.Core.Ffmpeg;
using MediaForge.Infrastructure.Configuration;
using MediaForge.Infrastructure.Diagnostics;
using MediaForge.Infrastructure.Ffmpeg;
using MediaForge.Infrastructure.Output;

namespace MediaForge.App.Services;

public sealed class ApplicationServices : IDisposable
{
    private ApplicationServices(
        IApplicationPaths applicationPaths,
        IApplicationLogger logger,
        IApplicationSettingsStore settingsStore,
        IQueueStore queueStore,
        IUserPresetStore userPresetStore,
        IJobManifestStore jobManifestStore,
        TemporaryOutputRecoveryService temporaryOutputRecoveryService,
        IConversionRunner conversionRunner,
        IFfmpegToolResolver ffmpegToolResolver,
        IFfmpegVersionReader ffmpegVersionReader,
        IFfmpegCapabilityService ffmpegCapabilityService,
        IHardwareEncoderProbe hardwareEncoderProbe,
        IFfmpegProcessRunner ffmpegProcessRunner,
        LocalizationService localization,
        ThemeService theme,
        SingleInstanceService singleInstance)
    {
        ApplicationPaths = applicationPaths;
        Logger = logger;
        SettingsStore = settingsStore;
        QueueStore = queueStore;
        UserPresetStore = userPresetStore;
        JobManifestStore = jobManifestStore;
        TemporaryOutputRecoveryService = temporaryOutputRecoveryService;
        ConversionRunner = conversionRunner;
        FfmpegToolResolver = ffmpegToolResolver;
        FfmpegVersionReader = ffmpegVersionReader;
        FfmpegCapabilityService = ffmpegCapabilityService;
        HardwareEncoderProbe = hardwareEncoderProbe;
        FfmpegProcessRunner = ffmpegProcessRunner;
        Localization = localization;
        Theme = theme;
        SingleInstance = singleInstance;
    }

    public IApplicationPaths ApplicationPaths { get; }

    public IApplicationLogger Logger { get; }

    public IApplicationSettingsStore SettingsStore { get; }

    public IQueueStore QueueStore { get; }

    public IUserPresetStore UserPresetStore { get; }

    public IJobManifestStore JobManifestStore { get; }

    public TemporaryOutputRecoveryService TemporaryOutputRecoveryService { get; }

    public IConversionRunner ConversionRunner { get; }

    public IFfmpegToolResolver FfmpegToolResolver { get; }

    public IFfmpegVersionReader FfmpegVersionReader { get; }

    public IFfmpegCapabilityService FfmpegCapabilityService { get; }

    public IHardwareEncoderProbe HardwareEncoderProbe { get; }

    public IFfmpegProcessRunner FfmpegProcessRunner { get; }

    public LocalizationService Localization { get; }

    public ThemeService Theme { get; }

    public SingleInstanceService SingleInstance { get; }

    public static ApplicationServices Create(string baseDirectory)
    {
        var applicationPaths = PortableApplicationPaths.Create(baseDirectory);
        var logger = new JsonLineApplicationLogger(applicationPaths);
        var processRunner = new FfmpegProcessRunner();
        var manifestStore = new JobManifestStore(applicationPaths, new AtomicJsonFileStore());
        return new ApplicationServices(
            applicationPaths,
            logger,
            new ApplicationSettingsStore(applicationPaths, new AtomicJsonFileStore()),
            new QueueStore(applicationPaths, new AtomicJsonFileStore()),
            new UserPresetStore(applicationPaths, new AtomicJsonFileStore()),
            manifestStore,
            new TemporaryOutputRecoveryService(manifestStore, new ManifestTemporaryOutputCleaner()),
            new FfmpegConversionRunner(processRunner, new OutputFileCommitter(), manifestStore),
            new FfmpegToolResolver(applicationPaths.BaseDirectory),
            new FfmpegVersionReader(),
            new FfmpegCapabilityService(applicationPaths, new AtomicJsonFileStore(), processRunner),
            new HardwareEncoderProbe(),
            processRunner,
            new LocalizationService(),
            new ThemeService(),
            SingleInstanceService.Acquire());
    }

    public void Dispose()
    {
        if (Logger is IDisposable disposableLogger)
        {
            disposableLogger.Dispose();
        }
        SingleInstance.Dispose();
    }
}
