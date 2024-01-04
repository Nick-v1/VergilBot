using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Vergil.Services.Enums;
using Vergil.Services.Misc;
using Vergil.Services.Modules;
using Vergil.Services.Services;
using Vergil.Services.Validation;

namespace Vergil.Main.Commands;

public class SlashCommands
{
    private DiscordSocketClient _client;
        private CommandService _commands;
        private ChatGpt _chatGpt;
        private IUserService _userService;
        private readonly IDiceService _dice;
        private readonly IStableDiffusion _stableDiffusion;
        private readonly IStableDiffusionValidator _stableDiffusionValidator;
        private readonly IUserValidationService _userValidation;
        private readonly IStripeService _stripeService;

        public SlashCommands(DiscordSocketClient client, CommandService commands, ChatGpt chatGptInstance, 
            IUserService userService, IDiceService diceService, IStableDiffusion stableDiffusion, 
            IStableDiffusionValidator diffusionValidator, IUserValidationService userValidationService, IStripeService stripeService) 
        { 
            _client = client;
            _commands = commands;
            _chatGpt = chatGptInstance;
            _userService = userService;
            _dice = diceService;
            _stableDiffusion = stableDiffusion;
            _stableDiffusionValidator = diffusionValidator;
            _userValidation = userValidationService;
            _stripeService = stripeService;
        }

        public async Task InstallSlashCommandsAsync()
        {
            await ClientReaderSlashCommands();
        }

        public async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name.Equals("quote"))
            {
                var vergilquote = new VergilQuotes();
                var quote = vergilquote.getQuote();
                await command.RespondAsync(quote);
                return;
            }
            
            if (command.Data.Name.Equals("weather"))
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
                    await command.RespondAsync($"{e.Message}");
                    /*switch (e.StatusCode)
                    {
                        case System.Net.HttpStatusCode.ServiceUnavailable:
                            await command.RespondAsync("503 Service Unavailable");
                            break;
                        case System.Net.HttpStatusCode.NotFound:
                            await command.RespondAsync($"Not Found");
                            break;
                        default:
                            await command.RespondAsync($"{e.Message}");
                            break;
                    }*/
                }
                return;

            }
            
            if (command.Data.Name.Equals("help"))
            {
                var e = _client.GetGlobalApplicationCommandsAsync().Result;
                var normalcommands = _commands.Commands;
                StringBuilder s = new StringBuilder();
                var slashoptionsElement = "";

                s.AppendLine("Slash Commands:\n");
                foreach (var c in e)
                {
                    if (c.Options.Count > 0)
                    {
                        var slashoptions = c.Options.ElementAt(0);
                        slashoptionsElement = slashoptions.Name;
                        s.AppendLine($"Usage: /**{c.Name}** _{slashoptionsElement}_. {c.Description}");
                        continue;
                    }
                    s.AppendLine($"Usage: /**{c.Name}**. {c.Description}");
                }

                s.AppendLine("\n_Prefix commands not showing. Legacy commands_");
                var emb = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription(s.ToString())
                    .WithCurrentTimestamp()
                    .WithFooter($"{_client.CurrentUser.Username}#{_client.CurrentUser.Discriminator}", _client.CurrentUser.GetAvatarUrl())
                    .WithTitle("Available Commands");

                await command.RespondAsync(embed: emb.Build());
                return;
            }
            
            if (command.Data.Name.Equals("banmepls"))
            {
                var e = command.User as IUser;
                var guild = _client.GetGuild(command.GuildId.Value);
                var channel = command.Channel;


                var EmbedBuilderLog = new EmbedBuilder()
                    .WithDescription($"User: {e.Username} just banned themselves! <:peepoSad:648843706337722402> ")
                    .WithColor(Color.Red);
                Embed embedLog = EmbedBuilderLog.Build();

                await guild.AddBanAsync(e, 1, "IM LEAVING :RAGEY:");
                await channel.SendMessageAsync($"{e.Username} just committed Sudoku! <:peepoSad:648843706337722402> ", embed: embedLog);
                await guild.RemoveBanAsync(e);
                return;
            }
            
            if (command.Data.Name.Equals("leledometro"))
            {

                var freedomDay = new DateTime(2023, 06, 08);
                var today = DateTime.Today;

                TimeSpan days = freedomDay - today;
                await command.RespondAsync($"Your mandatory chore ends in: {(days.Days).ToString()} days.");


                return;
            }
            
            if (command.Data.Name.Equals("reddit"))
            {

                var subreddit = command.Data.Options.First().Value.ToString();
                IUser botUser = _client.CurrentUser;

                var reddit = new Reddit(subreddit, botUser);

                var info = await reddit.sendRequest();

                await command.RespondAsync(embed: info.Build());

                return;

            }
            
            if (command.Data.Name.Equals("register"))
            {
                try
                {
                    IUser user = command.User;

                    var embedReply = await _userService.Register(user);

                    await command.RespondAsync(embed: embedReply);
                }
                catch (Exception e)
                {
                    await command.RespondAsync(embed: new EmbedBuilder().WithDescription(e.Message).Build());
                }
            }
            
            if (command.CommandName.Equals("balance"))
            {
                try
                {
                    var user = command.User as IUser;
                    await command.DeferAsync();

                    var balance = await _userService.GetBalance(user);

                    await command.FollowupAsync(embed: balance);

                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            
            if (command.Data.Name.Equals("deposit"))
            {
                if (!command.User.Id.Equals(223127328048414721))
                {
                    await command.RespondAsync("You are not allowed to use this command");
                    return;
                }
                try
                {
                    await command.DeferAsync();
                    IUser discordUser = command.User;
                    var fundsToAdd = Decimal.Parse(command.Data.Options.ElementAt(0).Value.ToString()!);

                    var (validation, user) = await _userValidation.ValidateUserExistence(discordUser);

                    if (!validation.Success)
                    {
                        await command.FollowupAsync("You are not registered.");
                        return;
                    }

                    var embedbalance = await _userService.Transact(user!, TransactionType.Deposit, PurchaseType.Bloodstones, fundsToAdd);

                    await command.FollowupAsync(embed: embedbalance);

                    return;
                }
                catch (Exception e)
                {
                    await command.FollowupAsync(e.Message);
                }
            }
            
            if (command.Data.Name.Equals("dice"))
            {
                try
                {
                    await command.DeferAsync();
                    var bet = Decimal.Parse(command.Data.Options.ElementAt(0).Value.ToString());

                    var user = command.User;

                    var result = await _dice.StartDice1(user, bet);

                    await command.FollowupAsync(embed: result);

                    return;
                }
                catch (Exception e)
                {
                    await command.FollowupAsync(e.Message);
                }
            }
            
            if (command.Data.Name.Equals("dice2"))
            {
                try
                {
                    await command.DeferAsync();
                    var bet = Decimal.Parse(command.Data.Options.ElementAt(0).Value.ToString());
                    var chances = (double)command.Data.Options.ElementAt(1).Value;

                    var user = command.User;

                    var result = await _dice.StartDice2(user, bet, chances);

                    await command.FollowupAsync(embed: result);

                    return;
                }
                catch (Exception e)
                {
                    await command.FollowupAsync(e.Message);
                }
            }
            
            if (command.CommandName.Equals("chat"))
            {  
                try
                {
                    var userInput = command.Data.Options.FirstOrDefault().Value.ToString();

                    await command.DeferAsync();

                    var response = await _chatGpt.TalkWithGpt(userInput);

                    if (response.Length <= 2000)
                    {
                        var embed1 = new EmbedBuilder()
                            .WithAuthor("GPT 3.5", "https://cdn.discordapp.com/attachments/673874082407907349/1145353361542086687/openai-icon.png")
                            .WithTitle($"{userInput}")
                            .WithDescription(response)
                            .WithCurrentTimestamp()
                            .WithColor(Color.DarkTeal)
                            .WithFooter($"{command.User.Username}", command.User.GetAvatarUrl())
                            .Build();
                        
                        await command.FollowupAsync(embed: embed1);
                        return;
                    }
                    
                    var embed2 = new EmbedBuilder()
                        .WithAuthor("GPT 3.5", "https://cdn.discordapp.com/attachments/673874082407907349/1145353361542086687/openai-icon.png")
                        .WithTitle($"{userInput}")
                        .WithDescription(response)
                        .WithCurrentTimestamp()
                        .WithColor(Color.Red)
                        .WithFooter($"{command.User.Username}", command.User.GetAvatarUrl())
                        .Build();

                    await command.FollowupAsync(embed: embed2);
                    return;
                    
                }
                catch (Exception ex)
                {
                    await command.FollowupAsync($"An error occured while processing your request. {ex.Message}");
                    return;
                }
                
            }
            
            if (command.CommandName.Equals("generate"))
            {
                try
                {
                    
                    await command.DeferAsync();

                    var discordUser = command.User;

                    var (userValidation, user) = await _userValidation.ValidateUserExistence(discordUser);

                    if (!userValidation.Success)
                    {
                        await command.FollowupAsync("Error: You are not registered.");
                        return;
                    }
                    
                    
                    if (user!.HasSubscription == false && user.Balance < 200 && user.GenerationTokens == 0)
                    {
                        await command.FollowupAsync("You have no balance to generate a picture.");
                        return;
                    }

                    var userInput = command.Data.Options.ElementAt(0).Value.ToString();

                    var optionsCount = command.Data.Options.Count;

                    if (optionsCount != 1)
                    {
                        if (optionsCount != 3)
                        {
                            await command.FollowupAsync("Error: Wrong number of parameters.");
                            return;
                        }
                        
                        var width = int.Parse(command.Data.Options.ElementAt(1).Value.ToString());
                        var height = int.Parse(command.Data.Options.ElementAt(2).Value.ToString());
                        
                        var validation = _stableDiffusionValidator.ValidateHeightAndWidth(width, height);
                        
                        if (!validation.Success)
                        {
                            var embed1 = new EmbedBuilder().WithColor(Color.DarkRed).WithTitle(validation.Message).Build();
                            
                            await command.FollowupAsync(embed: embed1);
                            return;
                        }
                        
                        var generatedImageBytes0 = await _stableDiffusion.GenerateImage(userInput, width: width, height: height);

                        if (user.HasSubscription == false)
                        {
                            if (user.GenerationTokens > 0)
                            {
                                await _userService.Transact(user, TransactionType.PaymentForService, PurchaseType.Tokens, 1);
                                Console.WriteLine($"Non Premium user: {user.Username} has created an image using paid tokens!");
                            }
                            else if (user.Balance > 200)
                            {
                                await _userService.Transact(user, TransactionType.PaymentForService, PurchaseType.Bloodstones, 200);
                                Console.WriteLine($"Non Premium user: {user.Username} has created an image using bloodstones!");
                            }
                        }
                        else
                        {
                            //Premium Members are not charged for now.
                            Console.WriteLine($"Premium user: {user.Username} has created an image!");
                        }

                        Console.WriteLine($"{user.Username} just created a picture in channel: {command.Channel} of guild: {command.GuildId}!");
                        
                        
                        var embed0 = new EmbedBuilder()
                            .WithTitle("Your image is ready")
                            .WithDescription(command.Data.Options.First().Value.ToString())
                            .WithImageUrl("attachment://generated_image.png")
                            .WithCurrentTimestamp()
                            .WithFooter($"{command.User.Username}", command.User.GetAvatarUrl())
                            .Build();

                        var memoryStream0 = new MemoryStream(generatedImageBytes0);

                        await command.FollowupWithFileAsync(memoryStream0, "generated_image.png", embed: embed0);
                        
                        return;
                    }

                    
                    var generatedImageBytes = await _stableDiffusion.GenerateImage(userInput, null, null);
                    if (user.HasSubscription == false)
                    {
                        if (user.GenerationTokens > 0)
                        {
                            await _userService.Transact(user, TransactionType.PaymentForService, PurchaseType.Tokens, 1);
                            Console.WriteLine($"Non Premium user: {user.Username} has created an image using paid tokens!");
                        }
                        else if (user.Balance > 200)
                        {
                            await _userService.Transact(user, TransactionType.PaymentForService, PurchaseType.Bloodstones, 200);
                            Console.WriteLine($"Non Premium user: {user.Username} has created an image using bloodstones!");
                        }
                    }
                    else
                    {
                        //Premium Members are not charged for now.
                        Console.WriteLine($"Premium user: {user.Username} has created an image!");
                    }
                    Console.WriteLine($"{user.Username} just created a picture in channel: {command.Channel} of guild: {command.GuildId}!");
                    
                    var embed = new EmbedBuilder()
                        .WithTitle("Your image is ready")
                        .WithDescription(command.Data.Options.First().Value.ToString())
                        .WithImageUrl("attachment://generated_image.png")
                        .WithCurrentTimestamp()
                        .WithFooter($"{command.User.Username}", command.User.GetAvatarUrl())
                        .Build();

                    var memoryStream = new MemoryStream(generatedImageBytes);

                    await command.FollowupWithFileAsync(memoryStream, "generated_image.png", embed: embed);
                    return;
                }
                catch (Exception ex)
                {
                    
                    if (ex.HResult.Equals(-2146233088))
                    {
                        await command.FollowupAsync("This service is temporarily unavailable.");
                        return;
                    }
                    
                    await command.FollowupAsync($"An error occured while processing your request. {ex.Message}");
                    return;
                }
            }
            
            if (command.CommandName.Equals("generateusingcontrolnet"))
            {
                try
                {
                    await command.DeferAsync();

                    var userPrompt = command.Data.Options.ElementAt(0).Value.ToString();

                    var attachment = command.Data.Options.ElementAt(1).Value;
                    
                    var attachmentOption = attachment as IAttachment;

                    if (attachmentOption.Url.Contains(".png"))
                    {

                        var generatedImageBytes = await _stableDiffusion.UseControlNet(userPrompt!, attachmentOption);

                        var embed = new EmbedBuilder()
                            .WithTitle($"Your image is ready (ControlNet)")
                            .WithDescription(command.Data.Options.First().Value.ToString())
                            .WithImageUrl("attachment://generated_image.png")
                            .WithCurrentTimestamp()
                            .WithFooter($"{command.User.Username}", command.User.GetAvatarUrl())
                            .Build();

                        var memoryStream = new MemoryStream(generatedImageBytes);

                        await command.FollowupWithFileAsync(memoryStream, "generated_image.png", embed: embed,
                            ephemeral: false);
                        
                    }
                    else
                    {
                        throw new Exception("The uploaded attachment is not a supported image format.");
                    }
                    

                }
                catch (Exception e)
                {
                    await command.FollowupAsync(e.Message);
                }
            }
            
            if (command.CommandName.Equals("img2img"))
            {
                try
                {
                    await command.DeferAsync();

                    var userPrompt = command.Data.Options.ElementAt(0).Value.ToString();
                    var userImage = command.Data.Options.ElementAt(1).Value;

                    var attachmentOption = userImage as IAttachment;

                    if (attachmentOption.Url.Contains(".png") || attachmentOption.Url.Contains(".jpg") ||
                        attachmentOption.Url.Contains(".jpeg"))
                    {
                        var generatedImageBytes = await _stableDiffusion.Img2Img(userPrompt, attachmentOption);
                        
                        var embed = new EmbedBuilder()
                            .WithTitle($"Your image is ready (Img2Img)")
                            .WithDescription(command.Data.Options.First().Value.ToString())
                            .WithImageUrl("attachment://generated_image.png")
                            .WithCurrentTimestamp()
                            .WithFooter($"{command.User.Username}", command.User.GetAvatarUrl())
                            .Build();

                        var memoryStream = new MemoryStream(generatedImageBytes);

                        await command.FollowupWithFileAsync(memoryStream, "generated_image.png", embed: embed,
                            ephemeral: false);
                    }
                    else
                    {
                        throw new Exception("The uploaded attachment is not a supported image format.");
                    }
                }
                catch (Exception e)
                {
                    await command.FollowupAsync(e.Message);
                }
            }

            if (command.CommandName.Equals("purchase"))
            {
                try
                {
                    await command.DeferAsync(true);
                    IUser discordUser = command.User;

                    var (validation, user) = await _userValidation.ValidateUserExistence(discordUser);

                    if (!validation.Success)
                    {
                        await command.FollowupAsync("You are not registered.");
                        return;
                    }

                    if (user!.Email is null)
                    {
                        await command.FollowupAsync("You need to register your email before making a purchase. Use /email to register your email.");
                        return;
                    }

                    var arg = command.Data.Options.ElementAt(0).Value;

                    if (arg.ToString()!.Contains("subscription"))
                    {
                        var link = await _stripeService.StripeTransaction(PurchaseType.Subscription, user.Email, null);
                        var embed = new EmbedBuilder()
                            .WithAuthor(_client.CurrentUser.Username, _client.CurrentUser.GetAvatarUrl())
                            .WithDescription($"You are purchasing a monthly subscription! Follow your stripe link: " +
                                             $"[Stripe]({link}) to complete your payment securely.")
                            .WithColor(Color.DarkTeal)
                            .Build();
                        await command.FollowupAsync(embed:embed);
                        return;
                    }
                    
                    if (arg.ToString()!.Contains("bloodstones"))
                    {
                        var link = await _stripeService.StripeTransaction(PurchaseType.Bloodstones, user.Email, arg.ToString());
                        var embed = new EmbedBuilder()
                            .WithAuthor(_client.CurrentUser.Username, _client.CurrentUser.GetAvatarUrl())
                            .WithDescription($"You are purchasing {arg}! Follow your stripe link: [Stripe]({link})")
                            .WithColor(Color.DarkTeal)
                            .Build();
                        await command.FollowupAsync(embed: embed);
                        return;
                    }
                    
                    if (arg.ToString()!.Contains("tokens"))
                    {
                        var link = await _stripeService.StripeTransaction(PurchaseType.Tokens, user.Email, arg.ToString());
                        var embed = new EmbedBuilder()
                            .WithAuthor(_client.CurrentUser.Username, _client.CurrentUser.GetAvatarUrl())
                            .WithDescription($"You are purchasing {arg}! Follow your stripe link: [Stripe]({link})")
                            .WithColor(Color.DarkTeal)
                            .Build();
                        await command.FollowupAsync(embed: embed);
                        return;
                    }

                }
                catch (Exception e)
                {
                    //await command.FollowupAsync("An error has occured. Try selecting one option at a time.");
                    await command.FollowupAsync(e.Message);
                }
            }

            if (command.Data.Name.Equals("email"))
            {
                try
                {
                    await command.DeferAsync(true);

                    var usersEmail = command.Data.Options.ElementAt(0).Value.ToString();
                    IUser discordUser = command.User;

                    var (validation, user) = await _userValidation.ValidateUserExistence(discordUser);

                    if (!validation.Success)
                    {
                        await command.FollowupAsync("You are not registered.");
                        return;
                    }

                    if (!Regex.IsMatch(usersEmail!, @"^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[a-zA-Z]{2,7}$"))
                    {
                        await command.FollowupAsync("Invalid Email Format.");
                        return;
                    }

                    var responseEmbed = await _userService.RegisterEmail(user!, usersEmail!);
                    await command.FollowupAsync(embed:responseEmbed);
                }
                catch (Exception e)
                {
                    await command.FollowupAsync("An error has occured.");
                }
            }
        }

        /// <summary>
        /// Use this method to chuck a message into smaller pieces.
        /// Only use if you exceed the maximum characters for normal messages of discord. (2000 characters).
        /// Embeds max value is 6000 so chucking is not needed.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="chunkSize"></param>
        /// <returns></returns>
        private List<string> ChunkString(string source, int chunkSize)
        {
            return Enumerable.Range(0, source.Length / chunkSize)
           .Select(i => source.Substring(i * chunkSize, chunkSize))
           .ToList();
        }

        private async Task ClientReaderSlashCommands()
        {
            var guild = _client.GetGuild(605772836660969493); //my guild

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

            var globalCommandBanYourself = new SlashCommandBuilder()
                .WithName("banmepls")
                .WithDescription("Use this to ban yourself from a server. Removes the ban shortly after.");

            var localCommandLeledometro = new SlashCommandBuilder()
                .WithName("leledometro")
                .WithDescription("Shows the remaining days of your military service");

            var globalReddit = new SlashCommandBuilder()
                .WithName("reddit")
                .WithDescription("Shows a random post from the chosen subreddit")
                .AddOption("subreddit", ApplicationCommandOptionType.String, "The subreddit you want", true);

            var deposit = new SlashCommandBuilder()
                .WithName("deposit")
                .WithDescription("Add funds to your account (alpha version)")
                .AddOption(new SlashCommandOptionBuilder()
                                .WithName("funds")
                                .WithDescription("Options")
                                .WithRequired(true)
                                .AddChoice("Add 50", 50.00)
                                .AddChoice("Add 200", 200.00)
                                .AddChoice("Add 1000", 1000.00)
                                .AddChoice("Add 10000", 10000.00)
                                .WithType(ApplicationCommandOptionType.Number));

            var registerCommand = new SlashCommandBuilder()
                .WithName("register")
                .WithDescription("register to the app");

            var diceCommand2 = new SlashCommandBuilder()
                .WithName("dice2")
                .WithDescription("Roll the dice. Choose your chances!")
                .AddOption("bet", ApplicationCommandOptionType.Number, "your bet size", true)
                .AddOption(new SlashCommandOptionBuilder()
                                .WithName("chances")
                                .WithDescription("pick your chances!")
                                .WithRequired(true)
                                .WithType(ApplicationCommandOptionType.Number));

            var diceCommand = new SlashCommandBuilder()
                .WithName("dice")
                .WithDescription("Roll the dice. Your chances are picked randomly!")
                .AddOption("bet", ApplicationCommandOptionType.Number, "your bet size", true);


            var balanceCommand = new SlashCommandBuilder()
                .WithName("balance")
                .WithDescription("Show your balance!");

            var chatGpt = new SlashCommandBuilder()
                .WithName("chat")
                .WithDescription("Chat with gpt 3.5")
                .AddOption("question", ApplicationCommandOptionType.String, "your question", true);

            var imageGeneration = new SlashCommandBuilder()
                .WithName("generate")
                .WithDescription("Generate a picture with today's random model. Costs 200 bloodstones.")
                .AddOption("prompt", ApplicationCommandOptionType.String, "your prompt", true)
                .AddOption("width", ApplicationCommandOptionType.Integer, "width of the image to be generated", false)
                .AddOption("height", ApplicationCommandOptionType.Integer, "height of the image to be generated", false);

            var controlNetGeneration = new SlashCommandBuilder()
                .WithName("generateusingcontrolnet")
                .WithDescription("Generate a picture with today's control network")
                .AddOption("prompt", ApplicationCommandOptionType.String, "your prompt", true)
                .AddOption("qr", ApplicationCommandOptionType.Attachment, "your qr code", true);

            var imageGenerationImg2Img = new SlashCommandBuilder()
                .WithName("img2img")
                .WithDescription("Change your image")
                .AddOption("prompt", ApplicationCommandOptionType.String, "your prompt", true)
                .AddOption("image", ApplicationCommandOptionType.Attachment, "your image", true);

            var purchaseCommand = new SlashCommandBuilder()
                .WithName("purchase")
                .WithDescription("Select an option:")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("funds")
                    .WithDescription("buy bloodstones")
                    .WithRequired(false)
                    .AddChoice("Buy 2000 for 2€", "2000 bloodstones")
                    .AddChoice("Buy 5000 for 5€", "5000 bloodstones")
                    .AddChoice("Buy 10000 for 10€", "10000 bloodstones")
                    .AddChoice("Buy 20000 for 19€", "20000 bloodstones")
                    .WithType(ApplicationCommandOptionType.String))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("subscription")
                    .WithDescription("buy a subscription")
                    .WithRequired(false)
                    .AddChoice("Buy monthly subscription (6.99€)", "monthly subscription")
                    .WithType(ApplicationCommandOptionType.String))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("tokens")
                    .WithDescription("buy image generation tokens")
                    .WithRequired(false)
                    .AddChoice("Buy 10 tokens for 1€", "10 tokens")
                    .AddChoice("Buy 100 tokens for 10€", "100 tokens")
                    .AddChoice("Buy 200 tokens for 20€", "200 tokens")
                    .WithType(ApplicationCommandOptionType.String));

            var registerEmailCommand = new SlashCommandBuilder()
                .WithName("email")
                .WithDescription("Register your email to the bot.")
                .AddOption("email", ApplicationCommandOptionType.String, "The email you want to use", true);

            try
            {
                await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(globalCommand1.Build());
                await _client.CreateGlobalApplicationCommandAsync(globalCommandHelp.Build());
                await _client.CreateGlobalApplicationCommandAsync(globalCommandBanYourself.Build());
                //await guild.CreateApplicationCommandAsync(localCommandLeledometro.Build());
                await _client.CreateGlobalApplicationCommandAsync(globalReddit.Build());
                await _client.CreateGlobalApplicationCommandAsync(deposit.Build());
                await _client.CreateGlobalApplicationCommandAsync(registerCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(diceCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(diceCommand2.Build());
                await _client.CreateGlobalApplicationCommandAsync(balanceCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(chatGpt.Build());
                await _client.CreateGlobalApplicationCommandAsync(imageGeneration.Build());
                await _client.CreateGlobalApplicationCommandAsync(controlNetGeneration.Build());
                await _client.CreateGlobalApplicationCommandAsync(imageGenerationImg2Img.Build());
                await _client.CreateGlobalApplicationCommandAsync(purchaseCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(registerEmailCommand.Build());
            }
            catch (HttpException e)
            {
                var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }
}