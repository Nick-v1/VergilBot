using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI_API;
using Discord;
using Discord.WebSocket;

namespace VergilBot.Modules
{
    public class ChatGpt
    {
        private string? openAiKey;

        public ChatGpt(IConfigurationRoot configurationRoot) 
        {
            openAiKey = configurationRoot.GetSection("OPENAI_TOKEN").Value;
        }


        public async Task<string?> TalkWithGpt(string userText)
        {
            var api = new OpenAIAPI(openAiKey);
            var chat = api.Chat.CreateConversation();

            chat.AppendUserInput(userText);

            string result = await chat.GetResponseFromChatbotAsync();

            return result;
        }

        /*public async Task<byte[]> GenerateImage(string prompt)
        {
            var api = new OpenAIAPI(openAiKey);

            var response = await api.ImageGenerations.CreateImageAsync(prompt);
            var imageUrl = response.Data[0].Url;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    await SaveImageLocally(imageBytes, $"generated_image_{Path.GetRandomFileName()}.png");
                    return imageBytes;
                }
                catch (Exception ex)
                {
                    // Handle the exception appropriately (e.g., logging, error handling)
                    Console.WriteLine($"Failed to download and save the image: {ex.Message}");
                    throw;
                }
            }
        }

        public async Task SaveImageLocally(byte[] imageBytes, string fileName)
        {
            try
            {
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedImages");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, fileName);
                await File.WriteAllBytesAsync(filePath, imageBytes);

                Console.WriteLine($"Image saved successfully at: {filePath}");
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately (e.g., logging, error handling)
                Console.WriteLine($"Failed to save the image: {ex.Message}");
                throw;
            }
        }*/
    }
}
