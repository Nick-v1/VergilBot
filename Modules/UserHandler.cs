using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VergilBot.Modules
{
    public class UserHandler
    {
        private DiscordSocketClient _client;

        public UserHandler(DiscordSocketClient _client) 
        { 
            this._client = _client;
        }

        public async Task UserJoin(SocketGuildUser user)
        {
            await user.AddRoleAsync(766202794864017428);

            var welcomechannel = _client.GetChannel(765952959581257758) as ISocketMessageChannel;
            var rules = _client.GetChannel(1048901442397818901) as ISocketMessageChannel;

            var embed = new EmbedBuilder().WithDescription($"Welcome to the server, {user.Mention} !\nMake sure to check the <#{rules.Id}> ~~and pick a color~~")
                .WithColor(Color.Blue)
                .WithCurrentTimestamp()
                .WithFooter($"{_client.CurrentUser.Username}#{_client.CurrentUser.Discriminator}", _client.CurrentUser.GetAvatarUrl())
                .WithAuthor($"{user.Username}#{user.Discriminator} joined the server!", user.GetAvatarUrl());

            await welcomechannel.SendMessageAsync(embed: embed.Build());

        }

        public async Task UserLeft(SocketGuild guild, SocketUser user)
        {
            var welcomechannel = _client.GetChannel(765952959581257758) as ISocketMessageChannel;

            var embed = new EmbedBuilder()
                .WithColor(Color.DarkMagenta)
                .WithFooter($"{user.Username} has left the server", user.GetAvatarUrl());

            await welcomechannel.SendMessageAsync(embed: embed.Build());
        }
    }
}
