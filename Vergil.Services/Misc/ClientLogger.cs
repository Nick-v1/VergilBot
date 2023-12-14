using Discord;

namespace Vergil.Services.Misc;

public class ClientLogger
{
    public ClientLogger() { }

    public Task ClientLog(LogMessage msg) 
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}