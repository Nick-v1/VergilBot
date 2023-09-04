using Discord;
using VergilBot.Models.Misc;
using VergilBot.Service.ValidationServices;
using VergilBot.Services.ValidationServices.EnumsAndResponseTemplate;

namespace VergilBot.Services;

public class DiceService : IDiceService
{
    private double chance;
    private readonly IUserValidationService _validation;
    private readonly IUserService _userService;
    const double maxChance = 100.0;
    const double minChance = 1.0;
    //const double defaultChance = 50.0;
    //const double defaultMultiplier = 2.0;
    
    public DiceService(IUserValidationService validationService, IUserService userService)
    {
        _validation = validationService;
        _userService = userService;
    }

    public async Task<Embed> StartDice1(IUser user, decimal bet)
    {
        var (validation, userReturned) = await _validation.ValidateUserExistence(user);
        var embed = new EmbedBuilder().WithCurrentTimestamp().WithAuthor("Dice Game", iconUrl: "https://img.icons8.com/arcade/256/dice.png");
        chance = ThreadLocalRandom.NewRandom().NextDouble() * maxChance;

        if (!validation.Success)
        {
            return embed.WithDescription(validation.Message).Build();
        }
        
        if (userReturned!.Balance < bet)
        {
            return embed.WithDescription("Your bet is higher than your balance!").WithColor(Color.Gold).Build();
        }

        var random = ThreadLocalRandom.NewRandom();
        var roll = random.NextDouble() * maxChance;

        bool win = roll <= chance;
        double payout = 0.0;

        embed.WithFooter($"Chances above: {roll:0.00}% win\n" +
                         $"Your chances: {chance:0.00}% of winning", user.GetAvatarUrl());

        double multiplier = minChance / (chance / maxChance);

        if (win)
        {
            payout = (double) bet * multiplier;
            var payoutAfterBet = payout - (double) bet;
            
            await _userService.Transact(user, TransactionType.WonBet, (decimal) payoutAfterBet);
            
            Console.WriteLine($"\n{user.Username}#{user.Discriminator} played with {bet} and won {payout} bloodstones with a multiplier of {multiplier:0.00}!");
            
            embed.WithDescription($"You have won {(payout):0.00} bloodstones! (Profit on win: {payoutAfterBet:0.00}).\n" +
                                  $"Winning multiplier: {multiplier:0.00}")
                .WithColor(Color.Green)
                .WithTitle("Win!");

            return embed.Build();
        }
        
        await _userService.Transact(user, TransactionType.LostBet, bet);
        
        Console.WriteLine($"\n{user.Username}#{user.Discriminator} lost the bet of {bet} bloostones. Potential multiplier: {multiplier:0.00}");

        var balance = await _userService.GetBalanceNormal(user);
        
        embed.WithDescription($"You have lost {bet} bloodstones. Your new balance is: {balance:0.00}.\n" +
                              $"Potential multiplier was: {multiplier:0.00}")
            .WithColor(Color.Red)
            .WithTitle("Lose");

        return embed.Build();
    }

    public async Task<Embed> StartDice2(IUser user, decimal bet, double chance)
    {
        var (validation, userReturned) = await _validation.ValidateUserExistence(user);
        var embed = new EmbedBuilder().WithCurrentTimestamp().WithAuthor("Dice Game", iconUrl: "https://img.icons8.com/arcade/256/dice.png");
        
        if (!validation.Success)
        {
            return embed.WithDescription(validation.Message).Build();
        }
        
        if (userReturned!.Balance < bet)
        {
            return embed.WithDescription("Your bet is higher than your balance!").WithColor(Color.Gold).Build();
        }

        var random = ThreadLocalRandom.NewRandom();
        var roll = random.NextDouble() * maxChance;

        bool win = roll <= chance;
        double payout = 0.0;
        
        embed.WithFooter($"Chances above: {roll:0.00}% win\n" +
                         $"Your chances: {chance:0.00}% of winning", user.GetAvatarUrl());

        double multiplier = minChance / (chance / maxChance);

        if (win)
        {
            payout = (double) bet * multiplier;
            var payoutAfterBet = payout - (double) bet;
            
            await _userService.Transact(user, TransactionType.WonBet, (decimal) payoutAfterBet);
            
            Console.WriteLine($"\n{user.Username}#{user.Discriminator} played with {bet} and won {payout} bloodstones with a multiplier of {multiplier:0.00}!");
            
            embed.WithDescription($"You have won {(payout):0.00} bloodstones! (Profit on win: {payoutAfterBet:0.00}).\n" +
                                  $"Winning multiplier: {multiplier:0.00}")
                .WithColor(Color.Green)
                .WithTitle("Win!");

            return embed.Build();
        }
        
        await _userService.Transact(user, TransactionType.LostBet, bet);
        
        Console.WriteLine($"\n{user.Username}#{user.Discriminator} lost the bet of {bet} bloostones. Potential multiplier: {multiplier:0.00}");

        var balance = await _userService.GetBalanceNormal(user);
        
        embed.WithDescription($"You have lost {bet} bloodstones. Your new balance is: {balance:0.00}.\n" +
                              $"Potential multiplier was: {multiplier:0.00}")
            .WithColor(Color.Red)
            .WithTitle("Lose");

        return embed.Build();
    }
}

public interface IDiceService
{
    Task<Embed> StartDice1(IUser user, decimal bet);
    Task<Embed> StartDice2(IUser user, decimal bet, double chance);
}