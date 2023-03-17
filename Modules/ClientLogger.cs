using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
