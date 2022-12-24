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
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using VergilBot.Models.Entities;
using VergilBot.Models.Misc;
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
        await _client.SetStatusAsync(UserStatus.Online);

        await _client.SetGameAsync("I AM THE STORM THAT IS APPROACHING", "", ActivityType.Listening);
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
        else if (command.Data.Name.Equals("weather"))
        {
            var city = command.Data.Options.First().Value.ToString();

            try
            {
                var HttpClientforWeather = new HttpClient();
                var responseString = await HttpClientforWeather.GetStringAsync($"https://goweather.herokuapp.com/weather/{city}");

                var weatherForecast = Weather.FromJson(responseString);
                //var nextDays = weatherForecast.Forecast;

                var emojify = new WeatherEmojify(weatherForecast, city);
                var resultstring = emojify.getEmojify();

                HttpClientforWeather.Dispose();
                await command.RespondAsync(resultstring);

            }
            catch (HttpRequestException e)
            {

                switch (e.StatusCode)
                {
                    case System.Net.HttpStatusCode.ServiceUnavailable:
                        await command.RespondAsync("503 Service Unavailable");
                        break;
                    case System.Net.HttpStatusCode.NotFound:
                        await command.RespondAsync($"City/Country/Area Not Found");
                        break;
                    default:
                        await command.RespondAsync($"{e.Message}");
                        break;
                }

            }
            return;

        }
        else if (command.Data.Name.Equals("help"))
        {
            var e = _client.GetGlobalApplicationCommandsAsync().Result;
            var normalcommands = _commands.Commands;
            StringBuilder s = new StringBuilder();
            var slashoptionsElement = "";

            s.AppendLine("Slash Commands:\n");
            foreach ( var c in e) 
            {
                if (c.Options.Count > 0)
                {
                    var slashoptions = c.Options.ElementAt(0);
                    slashoptionsElement = slashoptions.Name;
                    s.AppendLine($"Usage: /**{c.Name}** _{slashoptionsElement}_. {c.Description}.");
                    continue;
                }
                s.AppendLine($"Usage: /**{c.Name}**. {c.Description}.");
            }

            s.AppendLine("\n_Prefix commands not showing. To be rendered obsolete_");

            await command.RespondAsync(s.ToString());
        }
        
    }

    /// <summary>
    /// Slash commands creation here
    /// Runs only once
    /// </summary>
    /// <returns></returns>
    private async Task ClientReaderSlashCommands()
    {
        var guild = _client.GetGuild(605772836660969493);

        var globalCommand = new SlashCommandBuilder();
        globalCommand.WithName("quote");
        globalCommand.WithDescription("Sends a random Vergil quote");

        var globalCommand1 = new SlashCommandBuilder()
            .WithName("weather")
            .WithDescription("Get the weather in your city")
            .AddOption("city", ApplicationCommandOptionType.String, "The city you want the weather for");

        var globalCommandHelp = new SlashCommandBuilder()
            .WithName("help")
            .WithDescription("Shows available commands");


        try
        {
            await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            await _client.CreateGlobalApplicationCommandAsync(globalCommand1.Build());
            await _client.CreateGlobalApplicationCommandAsync(globalCommandHelp.Build());
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