using Discord;
using Vergil.Data.Models;
using Vergil.Services.Enums;
using Vergil.Services.Repositories;
using Vergil.Services.Validation;

namespace Vergil.Services.Services;

public interface IUserService
{
    Task<Embed> GetBalance(IUser user);
    Task<Embed> Register(IUser user);
    Task<Embed> Transact(User user, TransactionType typeOfTransaction, PurchaseType purchaseType, decimal amount);
    Task<decimal> GetBalanceNormal(IUser user);
    Task<Embed> RegisterEmail(User user, string email);
    Task<User?> GetUserAsync(IUser discordUser);
}

public class UserService : IUserService
{
    private readonly IUserRepository _user;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    public UserService(IUserRepository userRepository)
    {
        _user = userRepository;
    }

    public async Task<Embed> Register(IUser user)
    {
        try
        {
            await _semaphore.WaitAsync();
            var validation = await ValidateForRegistration(user);

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
                GenerationTokens = 0,
            };

            await _user.Register(userModel);
            _semaphore.Release();

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
            return new EmbedBuilder().WithTitle(e.Message).Build();
        }
    }

    public async Task<Embed> Transact(User user, TransactionType typeOfTransaction, PurchaseType purchaseType, decimal amount)
    {
        await _semaphore.WaitAsync();
        if (purchaseType == PurchaseType.Bloodstones)
        {
            if (typeOfTransaction.Equals(TransactionType.Deposit))
            {
                var newBalance = user.Balance + amount;
                await _user.TransactWithBalance(user, newBalance!); //PurchaseType.Bloodstones ensures that there's no null value.
                _semaphore.Release();
                return new EmbedBuilder().WithTitle("Successful Deposit").WithDescription($"{amount} bloodstones has been credited into your account!\n" +
                    $"You now have {user.Balance} bloodstones").WithColor(Color.Green).Build();
            }
        
            if (typeOfTransaction.Equals(TransactionType.Withdrawal))
            {
                throw new NotImplementedException();
            }
        
            if (typeOfTransaction.Equals(TransactionType.WonBet))
            {
                var newBalance = user.Balance + amount;
                await _user.TransactWithBalance(user, newBalance!);
                _semaphore.Release();
                return new EmbedBuilder()
                    .WithColor(Color.Green).WithCurrentTimestamp().Build();
            }

            if (typeOfTransaction.Equals(TransactionType.LostBet))
            {
                var newBalance = user.Balance - amount;
                await _user.TransactWithBalance(user, newBalance!);
                _semaphore.Release();
                return new EmbedBuilder().WithColor(Color.Red).WithCurrentTimestamp().Build();
            }

            if (typeOfTransaction.Equals(TransactionType.PaymentForService))
            {
                var newBalance = user.Balance - amount;
                await _user.TransactWithBalance(user, newBalance!);
                _semaphore.Release();
                return new EmbedBuilder().WithTitle("Service Paid.").WithDescription($"You have paid for a service.")
                    .WithColor(Color.Red).WithCurrentTimestamp().Build();
            }
        }
        else if (purchaseType == PurchaseType.Tokens)
        {
            if (typeOfTransaction == TransactionType.Deposit)
            {
                var newTokenBalance = user.GenerationTokens + amount;
                await _user.TransactWithBalance(user, (decimal) newTokenBalance!); 
                _semaphore.Release();
                return new EmbedBuilder().WithTitle("Successful Deposit").WithDescription($"{amount} bloodstones has been credited into your account!\n" +
                    $"You now have {user.GenerationTokens} Generation Tokens").WithColor(Color.Green).Build();
            }
            if (typeOfTransaction == TransactionType.PaymentForService)
            {
                var newTokenBalance = user.GenerationTokens - int.Parse(amount.ToString()!); //PurchaseType.Tokens also ensures there's no null value.
                await _user.TransactWithTokens(user, (int)newTokenBalance!);
                _semaphore.Release();
                return new EmbedBuilder().WithTitle("Service Paid.").WithDescription($"You have paid with tokens.")
                    .WithColor(Color.DarkGreen).WithCurrentTimestamp().Build();
            }
        }

        _semaphore.Release();
        throw new SystemException("Unhandled case");
    }

    public async Task<Embed> GetBalance(IUser user)
    {
        var (validation, returnedUser) = await ValidateUserExistence(user);

        if (!validation.Success)
        {
            return new EmbedBuilder().WithTitle(validation.Message).WithColor(Color.Red).WithFooter(user.Username, user.GetAvatarUrl()).Build();
        }

        var premiumMemberStatus = returnedUser!.HasSubscription ? "Enabled" : "Disabled";
        var embed = new EmbedBuilder()
            .WithAuthor(user.Username, user.GetAvatarUrl())
            .WithDescription($"\ud83d\udc8e\ud83e\ude78**Balance: {returnedUser.Balance:0.00}**\n" +
                             $"\u2b50\t**Premium Member: _{premiumMemberStatus}_**\n" +
                             $"\ud83c\udf9f\t**Generation Tokens: {returnedUser.GenerationTokens}**")
            .WithColor(Color.DarkTeal)
            .Build();
        
        return embed;
    }

    public async Task<Embed> RegisterEmail(User user, string email)
    {
        await _semaphore.WaitAsync();
        await _user.RegisterEmail(user, email);
        _semaphore.Release();
        var embed = new EmbedBuilder()
            .WithAuthor($"Your email has been registered")
            .WithColor(Color.DarkTeal)
            .Build();
        
        return embed;
    }

    public async Task<User?> GetUserAsync(IUser discordUser)
    {
        await _semaphore.WaitAsync();
        var user = await _user.GetUserById(discordUser.Id.ToString());
        _semaphore.Release();
        return user;
    }
    
    public async Task<decimal> GetBalanceNormal(IUser user)
    {
        var userReturned = await GetUserAsync(user);
        return userReturned.Balance;
    }
    
    private async Task<(ValidationReport, User?)> ValidateUserExistence(IUser discordUser)
    {
        var user = await GetUserAsync(discordUser);
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
    
    private async Task<ValidationReport> ValidateForRegistration(IUser discordUser)
    {
        var report = new ValidationReport();
        var user = await GetUserAsync(discordUser);

        if (user is not null)
        {
            report.Message = "You are already registered!";
            report.Success = false;
            return report;
        }

        report.Success = true;
        return report;
    }
    
}