using MediaForge.Core.Configuration;
using MediaForge.Core.Importing;

namespace MediaForge.Core.Tests;

public sealed class ConfigurationDocumentTests
{
    [Fact]
    public void Default_settings_use_safe_portable_values()
    {
        var settings = ApplicationSettings.CreateDefault();

        Assert.Equal(ApplicationSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.Equal(ApplicationLanguage.System, settings.Language);
        Assert.Equal(ApplicationTheme.System, settings.Theme);
        Assert.Null(settings.FfmpegDirectory);
        Assert.Equal(OutputConflictPolicy.AutoRename, settings.OutputConflictPolicy);
        Assert.Equal(1, settings.MaxConcurrentJobs);
        Assert.Equal(FileNameFilterMode.All, settings.LastFolderImport.FilterMode);
    }

    [Fact]
    public void Queue_document_normalizes_running_jobs_to_interrupted_on_restore()
    {
        var document = new QueueDocument(
            QueueDocument.CurrentSchemaVersion,
            [
                new PersistedConversionJob(Guid.NewGuid(), "input.mp4", "output.mp4", PersistedJobStatus.Running),
                new PersistedConversionJob(Guid.NewGuid(), "input2.mp4", "output2.mp4", PersistedJobStatus.Succeeded)
            ]);

        var restored = document.RestoreAfterApplicationStart();

        Assert.Equal(PersistedJobStatus.Interrupted, restored.Jobs[0].Status);
        Assert.Equal(PersistedJobStatus.Succeeded, restored.Jobs[1].Status);
    }
}
