using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VergilBot.Modules
{
    public class StableDiffusion
    {
        private string _url;
        private readonly int _steps;
        private readonly string _sampler;
        private readonly int _width;
        private readonly int _height;
        private readonly string _negativePrompt;
        private readonly string infoEndpoint;
        private readonly string txt2imgEndpoint;

        public StableDiffusion(string prompt)
        {
            _url = "http://127.0.0.1:7860";
            _sampler = "Euler a";
            _negativePrompt = "(low resolution, low quality, worst quality, normal quality)";
            infoEndpoint = $"{_url}/sdapi/v1/png-info";
            txt2imgEndpoint = $"{_url}/sdapi/v1/txt2img";
        }

        public async Task<byte[]?> GenerateImage(string prompt)
        {
            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "prompt", prompt },
                    { "steps", 24 },
                    { "sampler_index", _sampler},
                    { "width", 512},
                    { "height", 512},
                    { "use_async", true},
                    { "negative_prompt", _negativePrompt},
                    { "cfg_scale", 6}
                };

                        /*var overrideSettings = new Dictionary<string, object>
                        {
                            { "CLIP_stop_at_last_layers", 2 }
                        };

                        var overridePayload = new Dictionary<string, object>
                        {
                            { "override_settings", overrideSettings }
                        };

                        foreach (var item in overridePayload)
                        {
                            payload[item.Key] = item.Value;
                        }*/

                using var httpClient = new HttpClient();
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(txt2imgEndpoint, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

                foreach (var imageBase64 in GetImageList(responseObject["images"]))
                {
                    using var imageStream = new MemoryStream(Convert.FromBase64String(imageBase64.Split(",", 2)[0]));
                    var image = Image.FromStream(imageStream);

                    var pngPayload = new
                    {
                        image = "data:image/png;base64," + imageBase64
                    };

                    // Send POST request to endpoint
                    jsonPayload = JsonConvert.SerializeObject(pngPayload);
                    content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    response = await httpClient.PostAsync(infoEndpoint, content);
                    response.EnsureSuccessStatusCode();

                    responseContent = await response.Content.ReadAsStringAsync();
                    var pngInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

                    var parameters = pngInfo["info"].ToString();

                    var property = image.PropertyItems[0];
                    property.Type = 2;
                    property.Value = Encoding.ASCII.GetBytes(parameters);
                    property.Len = property.Value.Length;

                    image.SetPropertyItem(property);

                    await SaveImageLocally(image, $"generated_image_{Path.GetRandomFileName()}.png");

                    return imageStream.ToArray();
                }

                return null;

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task SaveImageLocally(Image image, string fileName)
        {
            try
            {
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedImages");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, fileName);


                ImageConverter converter = new ImageConverter();
                byte[] imageBytes = (byte[])converter.ConvertTo(image, typeof(byte[]));

                await File.WriteAllBytesAsync(filePath, imageBytes);

                Console.WriteLine($"Image saved successfully at: {filePath}");
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately (e.g., logging, error handling)
                Console.WriteLine($"Failed to save the image: {ex.Message}");
                throw;
            }
        }

        private List<string> GetImageList(object imagesValue)
        {
            if (imagesValue is JArray imagesArray)
            {
                return imagesArray.ToObject<List<string>>();
            }
            else if (imagesValue is List<string> imagesList)
            {
                return imagesList;
            }
            else
            {
                throw new InvalidOperationException("Invalid type for 'images' value.");
            }
        }
    }
}
