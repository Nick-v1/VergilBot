using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System.Reactive.Concurrency;
using System.Text;

namespace VergilBot.Modules
{
    /// <summary>
    /// Non slash commands
    /// </summary>
    public class Commands : ModuleBase<SocketCommandContext>
    {
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
