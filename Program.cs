using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VergilBot.Modules;

class Program
{
    private DiscordSocketClient _client;
    private IServiceCollection _collection;
    private CommandService _commands;
    private DiscordSocketConfig _config;
    private IServiceProvider _services;
    private static IConfigurationRoot configurationRoot;
    private slashCommands _slashCommands;

    public static void Main(string[] args) 
    {
        new Program().MainAsync().GetAwaiter().GetResult(); 
    }

    public static IConfigurationRoot GetConfiguration()
    {
        return configurationRoot;
    }

    public async Task MainAsync()
    {
        configurationRoot = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddUserSecrets<Program>().Build();

        _config = new DiscordSocketConfig() { GatewayIntents = GatewayIntents.All };

        _collection = new ServiceCollection()
            .AddSingleton(_config)
            .AddSingleton<DiscordSocketClient>();

        _services = _collection.BuildServiceProvider();
        
        _commands = new CommandService();

        _client = new DiscordSocketClient(_config);

        _slashCommands = new slashCommands(_client, _commands);

        
        var ClientLogger = new ClientLogger();
        _client.Log += ClientLogger.ClientLog;

        //use builder to get discord token;
        string token = configurationRoot.GetSection("DISCORD_TOKEN").Value;
        
        await InstallCommandsAsync();
        
        await _client.LoginAsync(TokenType.Bot, token); //GetEnvironmentVariable("token");
        await _client.StartAsync();


        await Task.Delay(Timeout.Infinite);
    }

    
    public async Task InstallCommandsAsync()
    {
        _client.MessageReceived += HandleCommandAsync;
        //_client.Ready += ClientReaderSlashCommands; //added to its own class
        //_client.SlashCommandExecuted += SlashCommandHandler; //added to its own class
        _client.Ready += ClientStatus;
        _client.UserJoined += UserJoin;
        
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        await _slashCommands.InstallSlashCommandsAsync(); //slashCommands.cs handles commands
    }

    private async Task UserJoin(SocketGuildUser user)
    {
        await user.AddRoleAsync(766202794864017428);
        
        var welcomechannel = _client.GetChannel(765952959581257758) as ISocketMessageChannel;
        var rules = _client.GetChannel(1048901442397818901) as ISocketMessageChannel;


        await welcomechannel.SendMessageAsync($"Welcome to the server, {user.Mention} !\nMake sure to check the <#{rules.Id}> ~~and pick a colour~~");
    }

    private async Task ClientStatus()
    {
        await _client.SetStatusAsync(UserStatus.Online);

        await _client.SetGameAsync("I AM THE STORM THAT IS APPROACHING", "", ActivityType.Listening);

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