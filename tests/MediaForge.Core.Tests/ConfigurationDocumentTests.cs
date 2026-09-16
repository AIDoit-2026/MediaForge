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
        Assert.Equal(1680, settings.WindowWidth);
        Assert.Equal(800, settings.WindowHeight);
    }

    [Fact]
    public void Queue_document_normalizes_running_jobs_to_interrupted_on_restore()
    {
        var document = new QueueDocument(
            QueueDocument.CurrentSchemaVersion,
            [
                new PersistedConversionJob(
                    Guid.NewGuid(),
                    "input.mp4",
                    "output.mp4",
                    PersistedJobStatus.Running,
                    ConversionParameterSnapshot.CreateDefault()),
                new PersistedConversionJob(
                    Guid.NewGuid(),
                    "input2.mp4",
                    "output2.mp4",
                    PersistedJobStatus.Succeeded,
                    ConversionParameterSnapshot.CreateDefault())
            ]);

        var restored = document.RestoreAfterApplicationStart();

        Assert.Equal(PersistedJobStatus.Interrupted, restored.Jobs[0].Status);
        Assert.Equal(PersistedJobStatus.Succeeded, restored.Jobs[1].Status);
    }

    [Fact]
    public void Preset_document_has_a_stable_schema_and_parameter_snapshot()
    {
        var preset = new PresetDocument(
            PresetDocument.CurrentSchemaVersion,
            Guid.NewGuid(),
            "Custom MP4",
            new ConversionParameterSnapshot("mp4", new Dictionary<string, string>
            {
                ["videoEncoder"] = "libx264"
            }));

        Assert.Equal(1, preset.SchemaVersion);
        Assert.Equal("mp4", preset.Parameters.OutputContainer);
        Assert.Equal("libx264", preset.Parameters.Values["videoEncoder"]);
    }

    [Fact]
    public void Built_in_presets_have_stable_ids_and_expected_conversion_intent()
    {
        var presets = BuiltInPresetCatalog.All;

        Assert.Collection(
            presets,
            youtube =>
            {
                Assert.Equal("YouTube 1080p", youtube.Name);
                Assert.Equal("mp4", youtube.Parameters.OutputContainer);
            },
            archive =>
            {
                Assert.Equal("High Quality Archive", archive.Name);
                Assert.Equal("mkv", archive.Parameters.OutputContainer);
            },
            audioOnly =>
            {
                Assert.Equal("Audio Only", audioOnly.Name);
                Assert.Equal("none", audioOnly.Parameters.Values["videoMode"]);
            },
            mp3 =>
            {
                Assert.Equal("MP3 Audio", mp3.Name);
                Assert.Equal("mp3", mp3.Parameters.OutputContainer);
                Assert.Equal("libmp3lame", mp3.Parameters.Values["audioEncoder"]);
            });

        Assert.Equal(4, presets.Select(preset => preset.Id).Distinct().Count());
    }
}
