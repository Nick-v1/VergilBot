using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    private DiscordSocketClient _client;
    private IServiceCollection _collection;
    private CommandService _commands;
    private DiscordSocketConfig _config;
    private IServiceProvider _services;
    private static IConfigurationRoot builder;

    private static void Main(string[] args) 
    {
        //builder to access client secrets
        builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddUserSecrets<Program>().Build();

       
        new Program().MainAsync().GetAwaiter().GetResult(); 
    
    }


    public async Task MainAsync()
    {
        
        _config = new DiscordSocketConfig() { GatewayIntents = GatewayIntents.All };

        _collection = new ServiceCollection()
            .AddSingleton(_config)
            .AddSingleton<DiscordSocketClient>();

        _services = _collection.BuildServiceProvider();

        _commands = new CommandService();

        _client = new DiscordSocketClient(_config);

        _client.Log += ClientLog;


        //use builder to get discord token;
        string token = builder.GetSection("DISCORD_TOKEN").Value;
        
        await InstallCommandsAsync();
        
        await _client.LoginAsync(TokenType.Bot, token); //GetEnvironmentVariable("token");
        await _client.StartAsync();


        await Task.Delay(Timeout.Infinite);
    }

    private Task ClientLog(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    public async Task InstallCommandsAsync()
    {
        _client.MessageReceived += HandleCommandAsync;
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    public async Task HandleCommandAsync(SocketMessage messageParam)
    {

        var message = messageParam as SocketUserMessage;

        if (message == null) return;

        int argPos = 6;
        
        //two ways of calling the bot
        if (message.HasStringPrefix("vergil ", ref argPos))
        {
            var context = new SocketCommandContext(_client, message);

            var result = await _commands.ExecuteAsync(context, argPos, _services);

            if (!result.IsSuccess)
            {
                Console.WriteLine(result.ErrorReason);
            }
            if (result.Error.Equals(CommandError.UnmetPrecondition))
            {
                await message.Channel.SendMessageAsync(result.ErrorReason);
            }
        }
        int argPos1 = 1;
        if (message.HasStringPrefix("v ", ref argPos1))
        {
            var context = new SocketCommandContext(_client, message);

            var result = await _commands.ExecuteAsync(context, argPos1, _services);

            if (!result.IsSuccess)
            {
                Console.WriteLine(result.ErrorReason);
            }
            if (result.Error.Equals(CommandError.UnmetPrecondition))
            {
                await message.Channel.SendMessageAsync(result.ErrorReason);
            }

        }
    }
}