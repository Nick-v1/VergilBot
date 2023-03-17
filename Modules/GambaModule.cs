using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VergilBot.Models.Misc;

namespace VergilBot.Modules
{
    internal class GambaModule
    {
        private double bet;
        private IUser user;
       
        private double userBalance;
       
        public GambaModule(double bet, IUser user)
        {
            this.bet = bet;
            this.user = user;
        }

        public EmbedBuilder StartGame()
        {
            CheckBalance();

            var embed = new EmbedBuilder().WithCurrentTimestamp().WithAuthor("Dice Game", iconUrl: "https://img.icons8.com/arcade/256/dice.png");

            if (userBalance.Equals(0.123456789))   // if balance then user doesn't exist in the db
                return embed.WithDescription("User not registered. Use /register to sign up!");

            if (userBalance <= 0 )
            {
                return embed.WithDescription("You have no balance to play").WithColor(Color.Gold);
            }

            if (userBalance < bet)
            {
                return embed.WithDescription("Your bet is higher than your balance!").WithColor(Color.Gold);
            }
            else
            {
                var random = ThreadLocalRandom.NewRandom();
                var randomNumberChoosen = random.Next(0, 100);

                embed.WithFooter($"RNG Number choosen: {randomNumberChoosen}");

                if (randomNumberChoosen >= 50)
                {
                    var sql = new elephantSql();
                    Console.WriteLine($"\n{user.Username}#{user.Discriminator} played with {bet} and won {bet * 2} bloodstones!");
                    sql.transact(user.Id.ToString(), "won bet", (bet * 2) - bet);
                    var newbalance = sql.CheckBalance(user.Id.ToString());

                    embed.WithDescription($"You have won {(bet * 2)} bloodstones! (Profit on win: {bet * 2 - bet}). Your new balance is: {newbalance}.")
                        .WithColor(Color.Green)
                        .WithTitle("Win!");

                    return embed;
                }
                else
                {
                    var sql = new elephantSql();
                    Console.WriteLine($"\n{user.Username}#{user.Discriminator} lost the bet of {bet} bloostones");
                    sql.transact(user.Id.ToString(), "lost bet", bet);
                    var newbalance = sql.CheckBalance(user.Id.ToString());

                    embed.WithDescription($"You have lost {bet} bloodstones. Your new balance is: {newbalance}.")
                        .WithColor(Color.Red)
                        .WithTitle("Lose");

                    return embed;
                }


            }
        }

        private void CheckBalance()
        {
            var elephDB = new elephantSql();
            userBalance = elephDB.CheckBalance(user.Id.ToString());
        }
    }
}
