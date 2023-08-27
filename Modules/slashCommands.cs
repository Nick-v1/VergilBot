using Discord.Net;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Text;
using VergilBot.Models.Entities;
using VergilBot.Models.Misc;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VergilBot.Modules
{
    public class slashCommands
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private ChatGpt _chatGpt;
        private IConfigurationRoot _configurationRoot;

        public slashCommands(DiscordSocketClient _client, CommandService _commands, ChatGpt chatGptInstance) 
        { 
            this._client = _client;
            this._commands = _commands;
            _chatGpt = chatGptInstance;
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
                var s = new elephantSql();
                IUser user = command.User;

                var result = s.Register(user);

                await command.RespondAsync(result);

                return;
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
            else if (command.CommandName.Equals("balance"))
            {
                var sql = new elephantSql();
                var userbalance = sql.CheckBalance(command.User.Id.ToString());

                EmbedBuilder embed = new EmbedBuilder()
                    .WithAuthor($"Your balance is {userbalance} bloostones.", command.User.GetAvatarUrl())
                    .WithColor(Color.DarkTeal);

                await command.RespondAsync(embed: embed.Build());

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
                    var userInput = command.Data.Options.FirstOrDefault().Value.ToString();

                    await command.DeferAsync();

                    var sd = new StableDiffusion(userInput);

                    var generatedImageBytes = await sd.GenerateImage(userInput);

                    var embed = new EmbedBuilder()
                        .WithTitle("Your image is ready")
                        .WithDescription(command.Data.Options.First().Value.ToString())
                        .WithImageUrl("attachment://generated_image.png")
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
                .WithDescription("Use our model to create an image")
                .AddOption("prompt", ApplicationCommandOptionType.String, "your prompt", true);

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
            }
            catch (HttpException e)
            {
                var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }
    }
}
