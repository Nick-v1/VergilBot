using System.Diagnostics;
using System.Globalization;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;
using VergilBot.Models.Misc;
using VergilBot.Repositories;
using VergilBot.Service.ValidationServices;
using VergilBot.Services;
using VergilBot.Services.ValidationServices.EnumsAndResponseTemplate;

namespace VergilBot.Modules
{
    /// <summary>
    /// Non slash commands
    /// </summary>
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private readonly IUserService _userService;
        private readonly IUserValidationService _validation;
        private static readonly Dictionary<string, int> symbolWeights = new Dictionary<string, int>
        {
            { "Apple", 30 },         // Higher weight (higher probability)
            { "Banana", 30 },        // Higher weight (higher probability)
            { "Cherry", 30 },        // Lower weight (lower probability)
            { "Lemon", 5 },        // Lower weight (lower probability)
            { "Strawberry", 3 },  // Lower weight (lower probability)
            { "Watermelon", 2 }    // Lower weight (lower probability)
        };
            
        private static readonly Dictionary<string, int> winMultipliers = new Dictionary<string, int>
        {
            { "Apple", 3 },
            { "Banana", 5 },
            { "Cherry", 10 },
            { "Lemon", 20 },
            { "Strawberry", 100 },
            { "Watermelon", 200 }
        };

        private readonly ISlotRepository _slot;

        public Commands(IUserService userService, IUserValidationService userValidationService, ISlotRepository slotRepository)
        {
            _userService = userService;
            _validation = userValidationService;
            _slot = slotRepository;
        }

        [Command("slots")]
        public async Task PlaySlots(int bet)
        {
            var iuser = Context.User as IUser;

            var (validation, user) = await _validation.ValidateUserExistence(iuser);

            if (!validation.Success)
            {
                await ReplyAsync("You are not registered: "+iuser.Mention);
                return;
            }

            var startBalance = user!.Balance;

            if (startBalance < bet)
            {
                await ReplyAsync("Your bet is higher than your balance. "+iuser.Mention);
                return;
            }

            if (bet > 10000)
            {
                await ReplyAsync("Your bet is too high. Max bet is: 1000" + iuser.Mention);
                return;
            }

            var progressiveJackPotDice = ThreadLocalRandom.NewRandom().Next(101);
            Console.WriteLine(progressiveJackPotDice);
            if (progressiveJackPotDice == 100)
            {
                Console.WriteLine($"User: {user.Username} has won the progressive jackpot!");
                var jackPotWin = await _slot.JackPotWin();
                var embed = new EmbedBuilder().WithTitle("Congratulations! <:PagMan:926833620423958529>").WithColor(Color.Gold)
                    .WithAuthor("Slots", @"https://cdn-icons-png.flaticon.com/512/287/287230.png")
                    .WithDescription($"**You have won the progressive jackpot** <:forsenCD:611176492205998091>\n" +
                                     $"Jackpot Win: **{jackPotWin:0.00} bloodstones! 💎🩸**")
                    .WithCurrentTimestamp()
                    .Build();
                await _userService.Transact(user, TransactionType.WonBet, decimal.Parse(jackPotWin.ToString(CultureInfo.CurrentCulture)));
                
                await ReplyAsync(embed: embed);
            }

            var weightedSymbols = new List<string>();
            foreach (var kvp in symbolWeights)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    weightedSymbols.Add(kvp.Key);
                }
            }

            var random = ThreadLocalRandom.NewRandom();
            var firstSymbol = weightedSymbols[random.Next(weightedSymbols.Count)];
            var secondSymbol = weightedSymbols[random.Next(weightedSymbols.Count)];
            var thirdSymbol = weightedSymbols[random.Next(weightedSymbols.Count)];

            Console.WriteLine($"Symbols: {firstSymbol}  {secondSymbol}  {thirdSymbol} ");
            
            if (firstSymbol == secondSymbol && secondSymbol == thirdSymbol)
            {
                var chosenSymbol = firstSymbol;

                // Get the win multiplier for the chosen symbol
                int winMultiplier = winMultipliers[chosenSymbol];
                

                // Replace text with emoji representation
                string chosenEmoji = GetEmojiRepresentation(chosenSymbol);
                
                // Calculate the winnings based on the bet and win multiplier
                double winnings = bet * winMultiplier;
                double winningsAfterBet = winnings - bet;

                Console.WriteLine($"User: {iuser.Username} just won: {winnings} bloodstones");
                
                  
                var surpriseMultiplier = ThreadLocalRandom.NewRandom().Next(1, 101);
                
                if (surpriseMultiplier == 100)
                {
                    var surprisedMultiplierValue = ThreadLocalRandom.NewRandom().Next(100, 10000);
                    Console.WriteLine($"{iuser.Username} just hit a lucky multiplier of: {surprisedMultiplierValue}");

                    await _userService.Transact(user, TransactionType.WonBet, (decimal)bet * (surprisedMultiplierValue + winMultiplier) - bet);
                    var balanceSurprise = await _userService.GetBalanceNormal(iuser);

                    var jackpotEmbed = CreateJackpotEmbed(chosenEmoji, winMultiplier, surprisedMultiplierValue, bet, balanceSurprise, iuser);
                    
                    await ReplyAsync(embed: jackpotEmbed.Build());
                    return;
                }

                var embed = await _userService.Transact(user, TransactionType.WonBet, (decimal)winningsAfterBet);
                var embedbuilder = embed.ToEmbedBuilder();
                
                embedbuilder.Title = $"{chosenEmoji} | {chosenEmoji} | {chosenEmoji}";
                embedbuilder.WithAuthor("Slots", @"https://cdn-icons-png.flaticon.com/512/287/287230.png");
                embedbuilder.WithFooter(Context.Client.CurrentUser.ToString(), Context.Client.CurrentUser.GetAvatarUrl());
                var balance = await _userService.GetBalanceNormal(iuser);
              

                
                embedbuilder.Description = $"Win: **{winnings} bloodstones**\n" +
                                           $"Symbol Multiplier: **{winMultiplier}x**\n" +
                                           $"Profit: **{winningsAfterBet} bloodstones**\n" +
                                           $"**Balance: {balance:0.00} 💎🩸**";

                Console.WriteLine();
                await ReplyAsync(embed: embedbuilder.Build());
            }
            else
            {
                Console.WriteLine($"User: {iuser.Username} just lost: {bet} bloodstones.");
                // Replace text with emoji representation
                string firstEmoji = GetEmojiRepresentation(firstSymbol);
                string secondEmoji = GetEmojiRepresentation(secondSymbol);
                string thirdEmoji = GetEmojiRepresentation(thirdSymbol);

                var embed = await _userService.Transact(user, TransactionType.LostBet, bet);
                var balance = await _userService.GetBalanceNormal(iuser);
                var embedbuilder = embed.ToEmbedBuilder();

                double jackpotValue = bet / 10.0f;
                double currentJackpot = await _slot.UpdateSlotStats(jackpotValue);

                embedbuilder.Title = $"{firstEmoji} | {secondEmoji} | {thirdEmoji}";
                embedbuilder.WithAuthor("Slots", @"https://cdn-icons-png.flaticon.com/512/287/287230.png");
                embedbuilder.WithFooter(Context.Client.CurrentUser.ToString(), Context.Client.CurrentUser.GetAvatarUrl());
                embedbuilder.Description = $"Progressive Jackpot: **{currentJackpot:0.00}**\n" +
                                           $"You have lost **{bet}** bloodstones.\n" +
                                           $"**Balance: {balance:0.00} 💎🩸**";
                Console.WriteLine();
                await ReplyAsync(embed: embedbuilder.Build());
            }
        }
        
        private string GetEmojiRepresentation(string symbol)
        {
            switch (symbol)
            {
                case "Apple":
                    return "🍏";
                case "Banana":
                    return "🍌";
                case "Cherry":
                    return "🍒";
                case "Lemon":
                    return "🍋";
                case "Strawberry":
                    return "🍓";
                case "Watermelon":
                    return "🍉";
                default:
                    return string.Empty;
            }
        }
        
        public EmbedBuilder CreateJackpotEmbed(string chosenEmoji, int winMultiplier, int surprisedMultiplierValue, int bet, decimal balanceSurprise, IUser iuser)
        {
            var embedbuilderSurprise = new EmbedBuilder();
            embedbuilderSurprise.Title = $"{chosenEmoji} | {chosenEmoji} | {chosenEmoji}  -  JACKPOT!";
            embedbuilderSurprise.WithAuthor("Slots", @"https://cdn-icons-png.flaticon.com/512/287/287230.png");
            embedbuilderSurprise.WithFooter(Context.Client.CurrentUser.ToString(), Context.Client.CurrentUser.GetAvatarUrl());
            embedbuilderSurprise.Description = $"**{iuser.Mention} just hit a jackpot! <:forsenCD:611176492205998091>**\n\n" +
                                               $"Symbol Multiplier: **{winMultiplier}x**\n" +
                                               $"Surprise Multiplier Activated! **{surprisedMultiplierValue}x**\n" +
                                               $"Final Multiplier: **{surprisedMultiplierValue + winMultiplier}x**\n" +
                                               $"Final Win: **{bet * (surprisedMultiplierValue + winMultiplier)} bloodstones**\n" +
                                               $"Profit: **{(bet * (surprisedMultiplierValue + winMultiplier)) - bet} bloodstones**\n" +
                                               $"\n**Balance: {balanceSurprise:0.00} 💎🩸**";
            embedbuilderSurprise.WithColor(Color.Gold);

            return embedbuilderSurprise;
        }

        
        [Command("ping")]
        public async Task Ping()
        {

            await ReplyAsync("pong");
        }

        [Command("say")]
        [Summary("Echoes a message.")]
        public Task SayAsync([Remainder][Summary("The text to echo")] string echo) => ReplyAsync(echo);

        [Command("button")]
        public async Task CreateNoButton(string buttonstr)
        {
            var builder = new ComponentBuilder().WithButton(buttonstr, "custom-button");
            var s = new SelectMenuOptionBuilder();
            await ReplyAsync("\t", components: builder.Build());
        }

        /*[Command("controlnet version")]
        public async Task ControlnetVersion()
        {
            var sd = new StableDiffusion();
            var result = await sd.TypeControlNet();

            var resultstring = "";

            foreach (var item in result)
            {
                resultstring += item;
            }
           
            var embd = new EmbedBuilder()
                .WithDescription(resultstring)
                .WithTitle("Available Controlnets:")
                .Build();

            await ReplyAsync(embed: embd);
        }*/

        [RequireOwner]
        [Command("start service SD")]
        public async Task StartSdService()
        {
            try
            {
                string batchFilePath = @"Z:\SUPER SD 2.0\stable-diffusion-webui\webui-user.bat";
                
                var process = new Process();
                process.StartInfo.FileName = @"cmd.exe";
                process.StartInfo.Arguments = $"/C \"{batchFilePath}\"";
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(batchFilePath);
                
                process.Start();

                await ReplyAsync("SD service has started");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Returns all created global slash commands
        /// </summary>
        /// <returns></returns>
        [Command("findglobalcommands")]
        public async Task FindGlobalCommands()
        {
            var s = Context.Client.GetGlobalApplicationCommandsAsync().Result;
            var s1 = "";


            foreach (var item in s)
            {
                s1 += $"{item.Name},  {item.Description}\n";
            }

            await ReplyAsync(s1);
        }

        /// <summary>
        /// Code to delete to specific global slash command
        /// </summary>
        /// <param name="commandname">string name of command</param>
        /// <returns>reply message</returns>
        [RequireOwner]
        [Command("deleteglobalcommand")]
        public async Task DeleteGlobalCommands(string commandname)
        {
            var s = Context.Client.GetGlobalApplicationCommandsAsync().Result;

            foreach (var item in s)
            {
                if (item.Name.Equals(commandname))
                {
                    await ReplyAsync("Element being deleted: " + item.Name);
                    await item.DeleteAsync();
                    await ReplyAsync("Deleted: " + item.Name);
                    return;
                }
            }


            //await ReplyAsync("Element being deleted: " + s.ElementAt(0).Name);
            //await s.ElementAt(0).DeleteAsync();

            await ReplyAsync(s.Count.ToString());
        }

        /// <summary>
        /// Returns all guild commands
        /// </summary>
        /// <returns></returns>
        [Command("findguildcommands")]
        public async Task FindGuildCommands()
        {
            var guild = Context.Guild;
            var commands = guild.GetApplicationCommandsAsync().Result;

            if (!(commands.Count == 0))
            {
                var s = "";

                foreach (var item in commands)
                {
                    s += $"{item.Name},  {item.Description}\n";
                }
                await ReplyAsync(s);
            }
            else {
                await ReplyAsync("No guild commands available");
            }
        }

        /// <summary>
        /// Deletes all guild (local) slash commands
        /// </summary>
        /// <returns></returns>
        [RequireOwner]
        [Command("deleteGuildCommands")]
        public async Task deleteGuildSlashCommands()
        {
            var guilde = Context.Guild;
            var commands = guilde.GetApplicationCommandsAsync().Result;
            StringBuilder sb = new StringBuilder();

            foreach (var item in commands)
            {
                sb.AppendLine("Deleted command: " + item.Name);
                item.DeleteAsync().Wait();
            }
            await ReplyAsync(sb.ToString());
        }

        [RequireOwner]
        [Command("deleteGuildCommand")]
        public async Task deleteGuildSlashCommand(string commandname)
        { 
            var guild = Context.Guild;
            var commands = guild.GetApplicationCommandsAsync().Result;

            foreach (var item in commands)
            {
                if (item.Name.Equals(commandname))
                {
                    await item.DeleteAsync();
                    await ReplyAsync("Deleted: "+item.Name+ " command");
                    return;
                }
            }
        }

        [Command("SDanalysis")]
        public async Task StableDiffusionResults()
        {
            await ReplyAsync("```sql\r\nWelcome to Stubble Diffusion channel for info\r\n\r\n\r\nFor better results\r\n\r\nDo not use:\r\n♦ Euler a (steps 30 WITH CFG Scale 20), \r\n♦ Euler a (steps 50 WITH CFG Scale 20),\r\n♦ LMS (steps 30 or less),\r\n♦ LMS (CFG Scale 9 or higher), \r\n♦ DPM2 (CFG Scale 20 or higher WITH steps 30 or less)\r\n♦ DLM2 a (CFG Scale 9 or higher WITH steps 30 or less)\r\n♦ DPM++ 2S a (CFG Scale 20 or higher WITH steps 30 or less)\r\n♦ DPM++ 2s a (CFG Scale 21 or higher WITH steps 50 or more)\r\n♦ DPM++ 2M (CFG Scale 20 or higher WITH steps 30 or less)\r\n♦ DPM fast\r\n♦ LMS Karras (CFG Scale 9 or higher WITH steps 30 or less)\r\n♦ LMS Karras (CFG Scale 11 or higher WITH steps 50 or more)\r\n♦ PLMS (steps less than 50)\r\n♦ PLMS (CFG Scale 10 or higher)\r\n↑**You will most likely get bad result if you use the above**↑\r\n------------------------------------------------------------\r\n\r\nBest all around:\r\n♥ Euler\r\n♥ Euler a\r\n♥ Heun\r\n♥ DPM2\r\n♥ DPM++ 2S a\r\n♥ DPM++ 2M\r\n♥ DPM Adaptive\r\n♥ DPM2 Karras\r\n♥ DPM2 a Karras\r\n♥ DPM++ 2S a Karras\r\n♥ DPM++ 2M Karras\r\n♥ DDIM \r\n↑**You will most likely get good result if you use the above**↑\r\n```");
        }

        [Command("SDtips")]
        public async Task SDTips()
        {
            await ReplyAsync("**Tips**:\nBegin your prompts with masterpiece, best quality, and add a short, descriptive sentence, such as a girl with an umbrella in the rain.\r\nStart with the following in the Negative prompts: lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, fewer digits, cropped, worst quality, low quality, normal quality, jpeg artifacts, signature, watermark, username, blurry, artist name\n\n**Extra tip**:\nTag weight: use parenthesis if you want to give greater meaning to a tag\nExample: (Blue sky) or ((Blue sky)) or (((Blue sky)))\nThe more parenthesis the greater the weight of the tag.\n");
        }

        [Command("SDnegativePrompts")]
        public async Task NegativePromptsSD()
        {
            await ReplyAsync("lowres, (bad anatomy, bad hands:1.1), text, error, missing fingers, extra digit, fewer digits, cropped, worst quality, low quality, normal quality, jpeg artifacts, signature, watermark, username, blurry, artist name, b&w, weird colors, (cartoon, 3d, bad art, poorly drawn, close up, blurry:1.5), (disfigured, deformed, extra limbs:1.5) ");
        }

        
        [Command("copypasta")]
        public async Task copypasta()
        {
            await ReplyAsync("**Negative Prompts copy pasta:**");
        }
        
    }

    [Group("Math")]
    public class SampleModule : ModuleBase<SocketCommandContext>
    {
        [Command("square")]
        [Summary("Squares a number.")]
        public async Task SquareAsync(
        [Summary("The number to square.")]
        int num)
        {
            // We can also access the channel from the Command Context.
            await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
        }

    }

    [Group("users")]
    public class UsersModule : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        [Alias("user", "whois")]
        public async Task UserInfoAsync(SocketUser user = null)
        {

            var userInfo = user ?? Context.Client.CurrentUser;   // if (user != null) userInfo = user else new Context(); //if whatever is to the left of is not null, use that, otherwise use what's to the right.
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");

        }

        [Command("hi")]
        public async Task SetUserStatusAsync()
        {
            var userInfo = Context.Message.Author;

            await userInfo.SendMessageAsync("hi ");
        }


        [Command("send")]
        public async Task SendMessageToUser(ulong userid, string messageToSend)
        {
            var user = Context.User;
            var usertarget = Context.Client.GetUser(userid);
            
            await usertarget.SendMessageAsync($"Hi {usertarget.Username}.\n" +
                $"{user.Username} has messaged you the following: {messageToSend}");


            await ReplyAsync($"Send Message to: {usertarget.Username}");
        }

        [Command("send")]
        public async Task SendMessageToUser(ulong userid, string messageToSend, string uservisibility)
        {
            
            var user = Context.User;
            var usertarget = Context.Client.GetUser(userid);
            var guild = Context.Guild;
            var catchannels = Context.Guild.CategoryChannels;
            var stringchanel = "Channels:---------------------------------\n";
            IReadOnlyCollection<SocketGuildChannel> channels = guild.Channels;
            IReadOnlyCollection<SocketGuildUser> users = guild.Users;

            foreach (var item in channels)
            {
                stringchanel += $"Channel name: {item.Name}, channel id: {item.Id}, Created At: {item.CreatedAt}.\n";
                
            }

           
            stringchanel += "\nUsers:-------------";
            foreach (var item in users)
            {
                stringchanel += $"\n{item.Username}, {item.Status}!";
            }

            if (uservisibility.Equals("invisible"))
            {
                Console.WriteLine($"Sent message to: {usertarget}. from: {user}. Message: {messageToSend}");
                await usertarget.SendMessageAsync(messageToSend);
            }
            else if (uservisibility.Equals("visible"))
            {
                Console.WriteLine($"Sent message to: {usertarget}. from: {user}. Message: {messageToSend}");
                await usertarget.SendMessageAsync($"Hi {usertarget.Username}.\n" +
                    $"{user.Username} has messaged you the following: {messageToSend}");
            }
            
            Console.WriteLine("-----------------------------\n" +
                $"Category Channels: {stringchanel}\n" +
                
                $"Current user: {guild.CurrentUser}\nDefault Channel: {guild.DefaultChannel}\n" +
                $"Default message Notifications: {guild.DefaultMessageNotifications}\n" +
                $"Description: {guild.Description}\nEmotes: {guild.Emotes.Count}\n" +
                $"Icon Url: {guild.IconUrl}\n" +
                $"Id: {guild.Id}\nName: {guild.Name}\nNsfw level: {guild.NsfwLevel}\n" +
                $"Owner: {guild.Owner}\nOwner Id: {guild.OwnerId}\n" +
                $"Rules: {guild.RulesChannel}\n" +
                $"Users: {guild.Users}");

            await ReplyAsync($"Sent Message to: {usertarget.Username}");
        }

        [Command("myinfo")]
        [Summary("Sends the user their account info")]
        public async Task SendUserInfo()
        {
            var userinfo = Context.Message.Author;
            await userinfo.SendMessageAsync($"Your info:\n" +
                $"Your id: {userinfo.Id}\n" +
                $"Your status: {userinfo.Status}\n" +
                $"Your avatar id: {userinfo.AvatarId}\n" +
                $"Your username: {userinfo.Username}\n" +
                $"Mutual Guilds: {userinfo.MutualGuilds.Count}\n" +
                $"Your active clients: {userinfo.ActiveClients.Count}\n" +
                $"Your username unique id: {userinfo.Discriminator}\n" +
                $"Bot account: {userinfo.IsBot}\n" +
                $"Public flags: {userinfo.PublicFlags}\n" +
                $"Your avatar Url: {userinfo.GetAvatarUrl(size: 256, format: ImageFormat.Auto).ToString()}");

            
        }
    }

    [Group("admin")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Group("clean")]
        public class CleanModule : ModuleBase<SocketCommandContext> 
        {
            [Command]
            public async Task DefaultCleanAsync()
            {
                var messages = Context.Channel.GetMessagesAsync(Context.Message, Direction.Around, 100).FlattenAsync();
                var m = await messages as IEnumerable<IMessage>;
                

                foreach (var message in m)
                {
                    await (Context.Channel as ITextChannel).DeleteMessageAsync(message);
                    //await (Context.Channel as IMessageChannel).DeleteMessageAsync(message);
                }
            }

            [Command("messages")]
            public async Task CleanAsync(int count)
            {
                var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Around, count).FlattenAsync();

                foreach (var message in messages)
                {
                    await Context.Channel.DeleteMessageAsync(message);
                }

            } 
        }

        [Group("purge")]
        public class PurgeModule : ModuleBase<SocketCommandContext> 
        { 
        
            [Command]
            public async Task PurgeAsync()
            {
                var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Around).FlattenAsync();
                var filteredMessages = messages.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays <= 14);

                if (filteredMessages.Count() == 0) await ReplyAsync("Messages must be younger than 2 weeks old");

                else
                {
                    await (Context.Channel as ITextChannel).DeleteMessagesAsync(filteredMessages);
                }
            }

            [Command("messages")]
            public async Task PurgeMessageAsync(int count)
            {
                var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, count).FlattenAsync();
                var filteredMessages = messages.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays <= 14);

                if (filteredMessages.Count() == 0) await ReplyAsync("Messages must be younger than 2 weeks old");

                await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
                
                
            }
        }
    }
}
