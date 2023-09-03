using Discord;
using VergilBot.Models;
using VergilBot.Repositories;
using VergilBot.Services.ValidationServices.EnumsAndResponseTemplate;

namespace VergilBot.Service.ValidationServices;

public class UserValidationService : IUserValidationService
{
    private readonly IUserRepository _repo;

    public UserValidationService(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task<ValidationReport> ValidateForRegistration(IUser discordUser)
    {
        var user = await _repo.GetUserById(discordUser.Id.ToString());
        var report = new ValidationReport();
        
        if (user is not null)
        {
            report.Success = false;
            report.Message = "User is already registered!";
            return report;
        }

        report.Success = true;
        return report;
    }

    public async Task<(ValidationReport, User?)> ValidateUserExistence(IUser discordUser)
    {
        var user = await _repo.GetUserById(discordUser.Id.ToString());
        var report = new ValidationReport();
        
        
        if (user is null)
        {
            report.Message = "User is not registered.";
            report.ErrorCode = ErrorCode.NotFound;
            report.Success = false;
            return (report, null);
        }

        report.Success = true;
        return (report, user);
    }
}

public interface IUserValidationService
{
    Task<ValidationReport> ValidateForRegistration(IUser discordUser);
    Task<(ValidationReport, User?)> ValidateUserExistence(IUser discordUser);
}