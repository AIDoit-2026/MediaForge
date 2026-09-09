namespace MediaForge.Core.Conversion;

public interface ICommandBuilder
{
    FfmpegCommandPlan Build(ConversionJobSpec job);
}
