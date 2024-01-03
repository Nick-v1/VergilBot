using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;
using Vergil.Data.Context;
using Vergil.Main.Commands;
using Vergil.Services.Misc;
using Vergil.Services.Modules;
using Vergil.Services.Repositories;
using Vergil.Services.Services;
using Vergil.Services.Validation;

namespace Vergil.Main;

public class Program
{
    private static IConfigurationRoot configuration;
    private DiscordSocketConfig _config;
    private IServiceCollection _collection;
    private IServiceProvider _services;
    private CommandService _commands;
    private DiscordSocketClient _client;
    private SlashCommands _slashCommands;
    
    public static async Task Main(string[] args) 
    {
        await new Program().MainAsync(); 
    }
    
    public static IConfigurationRoot GetConfiguration()
    {
        return configuration;
    }
    
    public async Task MainAsync()
    {
        configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        _config = new DiscordSocketConfig() { GatewayIntents = GatewayIntents.All };

        _collection = new ServiceCollection()
            .AddSingleton(_config)
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<DiscordSocketClient>()
            .AddDbContext<VergilDbContext>(options => options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Scoped)
            .AddScoped<IUserRepository, UserRepository>()
            .AddScoped<IUserService, UserService>()
            .AddScoped<SlashCommands>()
            .AddScoped<ChatGpt>()
            .AddScoped<CommandService>()
            .AddScoped<IUserValidationService, UserValidationService>()
            .AddScoped<IDiceService, DiceService>()
            .AddScoped<IStableDiffusion, StableDiffusion>()
            .AddScoped<IStableDiffusionValidator, StableDiffusionValidator>()
            .AddScoped<ISlotRepository, SlotRepository>()
            .AddScoped<IStripeService, StripeService>();

        StripeConfiguration.ApiKey = configuration.GetValue<string>("StripeSettings:SecretKey");
        
        _services = _collection.BuildServiceProvider();

        _commands = _services.GetRequiredService<CommandService>();

        _client = _services.GetRequiredService<DiscordSocketClient>();

        _slashCommands = _services.GetRequiredService<SlashCommands>();

        //use builder to get discord token from .net secrets.
        string token = configuration.GetSection("DISCORD_TOKEN").Value;

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

        if (message.Content.Contains("βιτσας") || message.Content.Contains("βίτσας") || message.Content.Contains("Βιτσας") 
            || message.Content.Contains("Βίτσας") || message.Content.Contains("Vitsas") || message.Content.Contains("vitsas") || 
            message.Content.Contains("zisopoulos") || message.Content.Contains("ζησόπουλος") )
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