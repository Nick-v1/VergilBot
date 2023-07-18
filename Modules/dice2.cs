using Discord;
using VergilBot.Models.Misc;

namespace VergilBot.Modules
{
    internal class dice2
    {
        private double bet;
        private IUser user;
        private double chance;
        private double userBalance;
        const double maxChance = 100.0;
        const double minChance = 1.0;

        public dice2(double bet, IUser user, double chance)
        {
            this.bet = bet;
            this.user = user;
            this.chance = chance;
        }

        public EmbedBuilder StartGame()
        {
            CheckBalance();

            var embed = new EmbedBuilder().WithCurrentTimestamp().WithAuthor("Dice Game", iconUrl: "https://img.icons8.com/arcade/256/dice.png");

            if (userBalance.Equals(0.123456789))   // if balance then user doesn't exist in the db
                return embed.WithDescription("User not registered. Use /register to sign up!");


            if (userBalance <= 0)
            {
                return embed.WithDescription("You have no balance to play").WithColor(Color.Gold);
            }

            if (chance <= 0 || chance > maxChance)
            {
                return embed.WithDescription("Invalid winning chance! Please enter a value between 1 and 100.");
            }

            if (userBalance < bet)
            {
                return embed.WithDescription("Your bet is higher than your balance!").WithColor(Color.Gold);
            }
            else
            {
                var random = ThreadLocalRandom.NewRandom();
                var roll = random.NextDouble() * maxChance;

                bool win = roll <= chance;
                double payout = 0.0;

                embed.WithFooter($"Chances above: {roll.ToString("0.00")}% win\n" +
                    $"Your chances: {chance.ToString("0.00")}% of winning", user.GetAvatarUrl());

                // calculates the correct winning multiplier
                double multiplier = minChance / (chance / maxChance);

                if (win)
                {

                    payout = bet * multiplier;
                    var payoutAfterBet = payout - bet;

                    var sql = new elephantSql();
                    Console.WriteLine($"\n{user.Username}#{user.Discriminator} played with {bet} and won {payout} bloodstones with a multiplier of {multiplier.ToString("0.00")}!");
                    sql.transact(user.Id.ToString(), "won bet", payoutAfterBet);
                    var newbalance = sql.CheckBalance(user.Id.ToString());

                    embed.WithDescription($"You have won {(payout).ToString("0.00")} bloodstones! (Profit on win: {payoutAfterBet.ToString("0.00")}).\n" +
                        $"Winning multiplier: {multiplier.ToString("0.00")}")
                        .WithColor(Color.Green)
                        .WithTitle("Win!");

                    return embed;
                }
                else
                {
                    var sql = new elephantSql();
                    Console.WriteLine($"\n{user.Username}#{user.Discriminator} lost the bet of {bet} bloostones. Potential multiplier: {multiplier.ToString("0.00")}");
                    sql.transact(user.Id.ToString(), "lost bet", bet);
                    var newbalance = sql.CheckBalance(user.Id.ToString());

                    embed.WithDescription($"You have lost {bet} bloodstones. Your new balance is: {newbalance}.\n" +
                        $"Potential multiplier was: {multiplier.ToString("0.00")}")
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
