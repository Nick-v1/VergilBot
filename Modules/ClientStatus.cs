using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VergilBot.Modules
{
    public class ClientStatus
    {
        private DiscordSocketClient _client;

        public ClientStatus(DiscordSocketClient client) 
        {
            this._client = client;
        }

        public async Task UpdateStatus()
        {
            await _client.SetStatusAsync(UserStatus.Online);
            await _client.SetGameAsync("I AM THE STORM THAT IS APPROACHING", "", ActivityType.Listening);
        }
    }
}
