using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VergilBot.Modules;
using VergilBot.Repositories;
using VergilBot.Service.ValidationServices;
using VergilBot.Services;
using VergilBot.Services.Context;

class Program
{
    private DiscordSocketClient _client;
    private IServiceCollection _collection;
    private CommandService _commands;
    private DiscordSocketConfig _config;
    private IServiceProvider _services;
    private static IConfigurationRoot configurationRoot;
    private slashCommands _slashCommands;

    public static async Task Main(string[] args) 
    {
        await new Program().MainAsync(); 
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
            .AddSingleton<IConfiguration>(configurationRoot)
            .AddSingleton<DiscordSocketClient>()
            .AddDbContext<VergilDbContext>(options => options.UseNpgsql(configurationRoot.GetConnectionString("DefaultConnection")), ServiceLifetime.Scoped)
            .AddTransient<IUserRepository, UserRepository>()
            .AddTransient<IUserService, UserService>()
            .AddSingleton<slashCommands>()
            .AddScoped<ChatGpt>()
            .AddSingleton<CommandService>()
            .AddTransient<IUserValidationService, UserValidationService>()
            .AddTransient<IDiceService, DiceService>()
            .AddTransient<IStableDiffusion, StableDiffusion>();

        _services = _collection.BuildServiceProvider();
        
        _commands = _services.GetRequiredService<CommandService>();

        _client = _services.GetRequiredService<DiscordSocketClient>();

        _slashCommands = _services.GetRequiredService<slashCommands>();

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
        
        _client.Ready += new ClientStatus(_client).UpdateStatus;

        UserHandler uh = new UserHandler(_client);

        _client.UserJoined += uh.UserJoin;

        _client.UserLeft += uh.UserLeft;

        _client.Log += new ClientLogger().ClientLog;

        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        _client.Ready += async () => await _slashCommands.InstallSlashCommandsAsync(); //slashCommands.cs handles commands
        
        
        _client.SlashCommandExecuted += async (command) =>
        {
            await Task.Run(() =>
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    _slashCommands.SlashCommandHandler(command);
                });
            });
        };
        
    }

    
    public async Task HandleCommandAsync(SocketMessage messageParam)
    {

        var message = messageParam as SocketUserMessage;

        if (message == null) return;

        int argPos = 6;

        if (message.Content.Contains("βιτσας") || message.Content.Contains("βίτσας") || message.Content.Contains("Βιτσας") || message.Content.Contains("Βίτσας") || message.Content.Contains("Vitsas") || message.Content.Contains("vitsas"))
            await message.Channel.SendMessageAsync("Don't say this name");

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