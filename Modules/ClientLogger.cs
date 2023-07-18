using Discord;

namespace VergilBot.Modules
{
    public class ClientLogger
    {
        public ClientLogger() { }

        public Task ClientLog(LogMessage msg) 
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
