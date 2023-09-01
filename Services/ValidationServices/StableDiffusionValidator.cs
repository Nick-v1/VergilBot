using VergilBot.Services.ValidationServices.EnumsAndResponseTemplate;

namespace VergilBot.Service.ValidationServices;

public class StableDiffusionValidator
{
    public StableDiffusionValidator()
    {
        
    }

    public ValidationReport ValidateHeightAndWidth(int width, int height)
    {
        var report = new ValidationReport();

        if (width > 1000 || height > 1000)
        {
            report.Message = "Width or Height should not be higher than 1000px";
            report.Success = false;
            report.ErrorCode = ErrorCode.InvalidHeightOrWidth;
            return report;
        }

        report.Success = true;
        return report;
    }
}