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
        IFfmpegToolResolver ffmpegToolResolver,
        IFfmpegVersionReader ffmpegVersionReader,
        IFfmpegCapabilityService ffmpegCapabilityService,
        IHardwareEncoderProbe hardwareEncoderProbe)
    {
        ApplicationPaths = applicationPaths;
        Logger = logger;
        SettingsStore = settingsStore;
        FfmpegToolResolver = ffmpegToolResolver;
        FfmpegVersionReader = ffmpegVersionReader;
        FfmpegCapabilityService = ffmpegCapabilityService;
        HardwareEncoderProbe = hardwareEncoderProbe;
    }

    public IApplicationPaths ApplicationPaths { get; }

    public IApplicationLogger Logger { get; }

    public IApplicationSettingsStore SettingsStore { get; }

    public IFfmpegToolResolver FfmpegToolResolver { get; }

    public IFfmpegVersionReader FfmpegVersionReader { get; }

    public IFfmpegCapabilityService FfmpegCapabilityService { get; }

    public IHardwareEncoderProbe HardwareEncoderProbe { get; }

    public static ApplicationServices Create(string baseDirectory)
    {
        var applicationPaths = PortableApplicationPaths.Create(baseDirectory);
        var logger = new JsonLineApplicationLogger(applicationPaths);
        return new ApplicationServices(
            applicationPaths,
            logger,
            new ApplicationSettingsStore(applicationPaths, new AtomicJsonFileStore()),
            new FfmpegToolResolver(applicationPaths.BaseDirectory),
            new FfmpegVersionReader(),
            new FfmpegCapabilityService(applicationPaths, new AtomicJsonFileStore()),
            new HardwareEncoderProbe());
    }

    public void Dispose()
    {
        if (Logger is IDisposable disposableLogger)
        {
            disposableLogger.Dispose();
        }
    }
}
