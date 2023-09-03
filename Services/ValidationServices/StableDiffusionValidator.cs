using NsfwSpyNS;
using VergilBot.Services.ValidationServices.EnumsAndResponseTemplate;

namespace VergilBot.Service.ValidationServices;

public class StableDiffusionValidator : IStableDiffusionValidator
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

    public void ClassifyImage(string path)
    {
        var ns = new NsfwSpy();
        var result = ns.ClassifyImage(@$"{path}");
        
        Console.WriteLine($"Results:\nHentai: {result.Hentai}\n" +
                          $"Neutral: {result.Neutral}\n" +
                          $"Sexy: {result.Sexy}\n" +
                          $"Pornography: {result.Pornography}\n" +
                          $"PredictedLabel: {result.PredictedLabel}\n" +
                          $"IsNsfw: {result.IsNsfw}");
    }
}

public interface IStableDiffusionValidator
{
    ValidationReport ValidateHeightAndWidth(int width, int height);
    void ClassifyImage(string path);
}