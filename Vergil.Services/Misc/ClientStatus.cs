using Discord;
using Discord.WebSocket;

namespace Vergil.Services.Misc;

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