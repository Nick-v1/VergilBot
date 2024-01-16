using Discord;
using Vergil.Data.Models;
using Vergil.Services.Enums;
using Vergil.Services.Services;

namespace Vergil.Services.Validation;

public interface IUserValidationService
{
    Task<(ValidationReport, User?)> ValidateUserExistence(IUser discordUser);
}

public class UserValidationService : IUserValidationService
{
    private readonly IUserService _userService;

    public UserValidationService(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<(ValidationReport, User?)> ValidateUserExistence(IUser discordUser)
    {
        var user = await _userService.GetUserAsync(discordUser);
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