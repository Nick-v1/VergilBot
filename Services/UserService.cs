using Discord;
using VergilBot.Models;
using VergilBot.Repositories;
using VergilBot.Service.ValidationServices;
using VergilBot.Services.ValidationServices.EnumsAndResponseTemplate;

namespace VergilBot.Services;

public interface IUserService
{
    Task<Embed> GetBalance(IUser user);
    Task<Embed> Register(IUser user);
    Task<Embed> Transact(IUser user, TransactionType typeOfTransaction, decimal balance);
    Task<decimal> GetBalanceNormal(IUser user);
}

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
                return new EmbedBuilder().WithTitle(validation.Message).WithFooter(user.Username, user.GetAvatarUrl()).WithCurrentTimestamp().WithColor(Color.Red).Build();
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

    public async Task<Embed> Transact(IUser user, TransactionType typeOfTransaction, decimal balance)
    {
        var (validation, userReturned) = await _validation.ValidateUserExistence(user);

        if (!validation.Success)
        {
            return new EmbedBuilder().WithTitle(validation.Message).WithFooter(user.Username, user.GetAvatarUrl()).WithCurrentTimestamp().WithColor(Color.Red).Build();
        }
        
        if (typeOfTransaction.Equals(TransactionType.Deposit))
        {
            var newBalance = userReturned!.Balance + balance;
            await _user.Transact(userReturned, newBalance);
            return new EmbedBuilder().WithTitle("Successful Deposit").WithDescription($"{balance} bloodstones has been credited into your account!\n" +
                $"You now have {userReturned.Balance} bloodstones").WithColor(Color.Green).Build();
        }
        
        if (typeOfTransaction.Equals(TransactionType.Withdrawal))
        {
            throw new NotImplementedException();
        }
        
        if (typeOfTransaction.Equals(TransactionType.WonBet))
        {
            var newBalance = userReturned!.Balance + balance;
            await _user.Transact(userReturned, newBalance);
            return new EmbedBuilder().WithTitle("Won Bet!").WithDescription($"You have won the bet!")
                .WithColor(Color.Green).WithCurrentTimestamp().WithFooter(user.Username, user.GetAvatarUrl()).Build();
        }

        if (typeOfTransaction.Equals(TransactionType.LostBet))
        {
            var newBalance = userReturned!.Balance - balance;
            await _user.Transact(userReturned, newBalance);
            return new EmbedBuilder().WithTitle("Lost Bet.").WithDescription($"You have lost the bet.")
                .WithColor(Color.Red).WithCurrentTimestamp().WithFooter(user.Username, user.GetAvatarUrl()).Build();
        }

        throw new SystemException("Unhandled case");
    }

    public async Task<Embed> GetBalance(IUser user)
    {
        var (validation, returnedUser) = await _validation.ValidateUserExistence(user);

        if (!validation.Success)
        {
            return new EmbedBuilder().WithTitle(validation.Message).WithColor(Color.Red).WithFooter(user.Username, user.GetAvatarUrl()).Build();
        }

        var embed = new EmbedBuilder()
            .WithAuthor($"Your balance is {returnedUser!.Balance:0.00} bloodstones.", user.GetAvatarUrl())
            .WithColor(Color.DarkTeal)
            .Build();
        
        return embed;
    }

    public async Task<decimal> GetBalanceNormal(IUser user)
    {
        var userReturned = await _user.GetUserById(user.Id.ToString());
        return userReturned.Balance;
    }
}


