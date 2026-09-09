using MediaForge.Core.Ffmpeg;
using MediaForge.Core.Media;

namespace MediaForge.Core.Conversion;

public interface IConversionValidator
{
    ConversionValidationResult Validate(
        MediaSourceInfo source,
        ConversionProfile profile,
        FfmpegCapabilities capabilities,
        IEnumerable<HardwareEncoderAvailability>? hardwareAvailability = null);
}
