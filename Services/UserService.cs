using Discord;
using VergilBot.Models;
using VergilBot.Repositories;
using VergilBot.Service.ValidationServices;

namespace VergilBot.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _user;
    private readonly IUserValidationService _validation;

    public UserService(IUserRepository userRepository, IUserValidationService validationService)
    {
        _user = userRepository;
        _validation = validationService;
    }

    public async Task<Embed> Register(IUser user)
    {
        try
        {
            var validation = await _validation.ValidateForRegistration(user);

            if (!validation.Success)
            {
                return new EmbedBuilder().WithTitle(validation.Message).WithFooter(user.Username, user.GetAvatarUrl()).WithCurrentTimestamp().Build();
            }
            
            
            var userModel = new User
            {
                Username = user.Username,
                DiscordId = user.Id.ToString(),
                Balance = 1000.5m,
                HasSubscription = false,
            };

            await _user.Register(userModel);

            var emb = new EmbedBuilder()
                .WithTitle("Successfully Registered")
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .WithDescription($"Username: {userModel.Username},\n" +
                                 $"Balance: {userModel.Balance},\n" +
                                 $"Discord ID: {userModel.DiscordId},\n" +
                                 $"Subscription: {userModel.HasSubscription}")
                .WithFooter($"{user.Username}", user.GetAvatarUrl())
                .Build();

            return emb;
        }
        catch (Exception e)
        {
            return new EmbedBuilder().WithTitle(e.Message.ToString()).Build();
        }
    }

    public async Task<Embed> GetBalance(IUser user)
    {

        var userDb = await _user.GetUserById(user.Id.ToString());

        var embed = new EmbedBuilder()
            .WithAuthor($"Your balance is {userDb.Balance} bloostones.", user.GetAvatarUrl())
            .WithColor(Color.DarkTeal)
            .Build();
        
        return embed;
    }
}

public interface IUserService
{
    Task<Embed> GetBalance(IUser user);
    Task<Embed> Register(IUser user);
}

