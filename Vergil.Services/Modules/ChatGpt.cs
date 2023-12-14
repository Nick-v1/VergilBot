using Microsoft.Extensions.Configuration;
using OpenAI_API;

namespace Vergil.Services.Modules;

public class ChatGpt
{
    private string? openAiKey;

    public ChatGpt(IConfiguration configuration) 
    {
        openAiKey = configuration.GetSection("OPENAI_TOKEN").Value;
    }


    public async Task<string?> TalkWithGpt(string userText)
    {
        var api = new OpenAIAPI(openAiKey);
        var chat = api.Chat.CreateConversation();

        chat.AppendUserInput(userText);

        string result = await chat.GetResponseFromChatbotAsync();

        return result;
    }
}