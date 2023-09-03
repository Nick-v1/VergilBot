using Discord.Net;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Text;
using VergilBot.Models.Entities;
using VergilBot.Models.Misc;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using VergilBot.Service.ValidationServices;
using VergilBot.Services;

namespace VergilBot.Modules
{
    public class slashCommands
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private ChatGpt _chatGpt;
        private IConfigurationRoot _configurationRoot;
        private IUserService _userService;

        public slashCommands(DiscordSocketClient _client, CommandService _commands, ChatGpt chatGptInstance, IUserService userService) 
        { 
            this._client = _client;
            this._commands = _commands;
            _chatGpt = chatGptInstance;
            _userService = userService;
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
            else if (command.Data.Name.Equals("help"))
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
            else if (command.Data.Name.Equals("banmepls"))
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
            else if (command.Data.Name.Equals("leledometro"))
            {

                var freedomDay = new DateTime(2023, 06, 08);
                var today = DateTime.Today;

                TimeSpan days = freedomDay - today;
                await command.RespondAsync($"Your mandatory chore ends in: {(days.Days).ToString()} days.");


                return;
            }
            else if (command.Data.Name.Equals("reddit"))
            {

                var subreddit = command.Data.Options.First().Value.ToString();
                IUser botUser = _client.CurrentUser;

                Reddit reddit = new Reddit(subreddit, botUser);

                var info = await reddit.sendRequest();

                await command.RespondAsync(embed: info.Build());

                return;

            }
            else if (command.Data.Name.Equals("register"))
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
            else if (command.CommandName.Equals("balance"))
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
            else if (command.Data.Name.Equals("deposit"))
            {
                var s = new elephantSql();
                IUser user = command.User;

                double fundsToAdd = (double)command.Data.Options.First().Value;

                s.transact(user.Id.ToString(), command.CommandName, fundsToAdd);
                var balance = s.CheckBalance(user.Id.ToString());

                var embedbalance = new EmbedBuilder()
                    .WithTitle("Successfully added!")
                    .WithDescription($"You have added: {fundsToAdd} bloodstones.\n" +
                    $"Your balance: {balance} bloodstones.")
                    .WithColor(Color.Teal)
                    .Build();

                await command.RespondAsync(embed: embedbalance);

                return;
            }
            else if (command.Data.Name.Equals("dice"))
            {
                var s = command.Data.Options.FirstOrDefault();
                //var chance = (double) command.Data.Options.ElementAtOrDefault(1).Value;

                //Console.WriteLine("Chance is:" +chance);

                var user = command.User;
                var bet = (double)s.Value;

                var gamba = new GambaModule(bet, user);
                var result = gamba.StartGame();



                await command.RespondAsync(embed: result.Build());

                return;
            }
            else if (command.Data.Name.Equals("dice2"))
            {
                var s = command.Data.Options.FirstOrDefault();
                var chance = (double)command.Data.Options.ElementAtOrDefault(1).Value;

                //Console.WriteLine("Chance is:" +chance);

                var user = command.User;
                var bet = (double)s.Value;

                var gamba = new dice2(bet, user, chance);
                var result = gamba.StartGame();



                await command.RespondAsync(embed: result.Build());

                return;
            }
            else if (command.CommandName.Equals("chat"))
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
            else if (command.CommandName.Equals("generate"))
            {

                try
                {
                    
                    await command.DeferAsync();
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
                        
                        var validation = new StableDiffusionValidator().ValidateHeightAndWidth(width, height);
                        if (!validation.Success)
                        {
                            var embed1 = new EmbedBuilder().WithColor(Color.DarkRed).WithTitle(validation.Message).Build();
                            
                            await command.FollowupAsync(embed: embed1);
                            return;
                        }

                        var sd0 = new StableDiffusion();
                        var generatedImageBytes0 = await sd0.GenerateImage(userInput, width: width, height: height);
                        
                        
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
                    
                    //await Task.Delay(3000);
                    
                    var sd = new StableDiffusion();

                    var generatedImageBytes = await sd.GenerateImage(userInput, null, null);
                    
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
            else if (command.CommandName.Equals("generateusingcontrolnet"))
            {
                try
                {
                    await command.DeferAsync();

                    var userPrompt = command.Data.Options.ElementAt(0).Value.ToString();

                    var attachment = command.Data.Options.ElementAt(1).Value;
                    
                    var attachmentOption = attachment as IAttachment;

                    if (attachmentOption.Url.EndsWith(".png"))
                    {
                        var sd = new StableDiffusion();

                        var generatedImageBytes = await sd.UseControlNet(userPrompt!, attachmentOption);

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
            else if (command.CommandName.Equals("img2img"))
            {
                try
                {
                    await command.DeferAsync();
                    var sd = new StableDiffusion();

                    var userPrompt = command.Data.Options.ElementAt(0).Value.ToString();
                    var userImage = command.Data.Options.ElementAt(1).Value;

                    var attachmentOption = userImage as IAttachment;

                    if (attachmentOption.Url.EndsWith(".png") || attachmentOption.Url.EndsWith(".jpg") ||
                        attachmentOption.Url.EndsWith(".jpeg"))
                    {
                        var generatedImageBytes = await sd.Img2Img(userPrompt, attachmentOption);
                        
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
                .WithDescription("Generate a picture with today's random model")
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
            }
            catch (HttpException e)
            {
                var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }
    }
}
