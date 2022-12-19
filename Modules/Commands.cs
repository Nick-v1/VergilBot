using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System.Reactive.Concurrency;
using System.Text;

namespace Vergil
{
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

                if (filteredMessages.Count() == 0) await ReplyAsync("Nothing to delete!");

                else
                {
                    await (Context.Channel as ITextChannel).DeleteMessagesAsync(filteredMessages);
                }
            }

            [Command("messages")]
            public async Task PurgeMessageAsync(int count)
            {
                var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, count).FlattenAsync();
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
            }
        }
    }
}
