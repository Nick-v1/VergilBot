using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.API;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using VergilBot.Modules;

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
        _client.Ready += ClientReaderSlashCommands;
        _client.Ready += ClientStatus;
        _client.SlashCommandExecuted += SlashCommandHandler;
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    private async Task ClientStatus()
    {
        await _client.SetStatusAsync(UserStatus.Idle);

        
    }

    /// <summary>
    /// Slash command handler here
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        if (command.Data.Name.Equals("quote"))
        {
            var vergilquote = new VergilQuotes();
            var quote = vergilquote.getQuote();
            await command.RespondAsync(quote);
            return;
        }
        await command.RespondAsync($"You executed {command.Data.Name}");
    }

    /// <summary>
    /// Slash commands here
    /// </summary>
    /// <returns></returns>
    private async Task ClientReaderSlashCommands()
    {
        var guild = _client.GetGuild(605772836660969493);

        var globalCommand = new SlashCommandBuilder();
        globalCommand.WithName("quote");
        globalCommand.WithDescription("Sends a random Vergil quote");

        try
        {
            await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
        }
        catch (HttpException e)
        {
            var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
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