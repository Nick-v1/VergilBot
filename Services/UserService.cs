using Discord;
using VergilBot.Models;
using VergilBot.Modules;

namespace VergilBot.Services;

public class UserService
{
    
    public UserService()
    {
        
    }

    public Embed Register(IUser user)
    {
        try
        {
            var userModel = new User
            {
                Username = user.Username,
                DiscordId = user.Id.ToString(),
                Balance = 1000.5m,
                HasSubscription = false,
            };

            var db = new elephantSql();
            var result = db.Register(userModel);

            if (result.Equals("You are already registered!"))
            {
                return new EmbedBuilder().WithTitle(result).Build();
            }

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

    public Embed GetBalance(IUser user)
    {
        var sql = new elephantSql();
        var userbalance = sql.CheckBalance(user.Id.ToString());

        
        if (userbalance.Equals(0.123456789d))
        {
            var embederror = new EmbedBuilder()
                .WithAuthor("You are not registered.")
                .WithColor(Color.Red)
                .Build();

            return embederror;
        }

        var embed = new EmbedBuilder()
            .WithAuthor($"Your balance is {userbalance} bloostones.", user.GetAvatarUrl())
            .WithColor(Color.DarkTeal)
            .Build();
        
        return embed;
    }
}

