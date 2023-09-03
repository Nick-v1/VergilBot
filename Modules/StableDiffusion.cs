using System.Drawing;
using System.Text;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VergilBot.Models.Entities;
using VergilBot.Service.ValidationServices;
using Image = System.Drawing.Image;

namespace VergilBot.Modules
{
    public interface IStableDiffusion
    {
        Task<byte[]?> GenerateImage(string prompt, int? width, int? height);
        Task<byte[]?> UseControlNet(string prompt, IAttachment userImage);
        Task<byte[]> Img2Img(string prompt, IAttachment userImage);
    }
    
    public class StableDiffusion : IStableDiffusion
    {
        private string _url;
        private readonly int _steps;
        private readonly string _sampler;
        private readonly int _width;
        private readonly int _height;
        private readonly string _negativePrompt;
        private readonly string infoEndpoint;
        private readonly string txt2imgEndpoint;
        private readonly string img2imgEndpoint;
        private readonly IStableDiffusionValidator _validator;

        public StableDiffusion(IStableDiffusionValidator diffusionValidator)
        {
            _url = "http://127.0.0.1:7860";
            _sampler = "DPM++ 2M Karras";
            _negativePrompt = "(worst quality:1.1), (low quality:1.1), (lowres:1.1), (monochrome:1.1), (greyscale), multiple views, comic, sketch, (blurry:1.1), transparent, " +
                              "easynegative, ng_deepnegative_v1_75t, (low quality, worst quality, lowres:1.1), text, patreon, watermark";
            infoEndpoint = $"{_url}/sdapi/v1/png-info";
            txt2imgEndpoint = $"{_url}/sdapi/v1/txt2img";
            img2imgEndpoint = $"{_url}/sdapi/v1/img2img";
            _validator = diffusionValidator;
        }
        public async Task<byte[]?> GenerateImage(string prompt, int? width, int? height)
        {
            
            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "prompt", $"{prompt}" },
                    { "steps", 28 },
                    { "sampler_index", "Restart"},
                    { "width", width ??= 600},
                    { "height", height ??= 600},
                    { "use_async", true},
                    { "negative_prompt", _negativePrompt},
                    { "cfg_scale", 7}
                };

                var overrideSettings = new Dictionary<string, object>
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
                }

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
                    byte[] imageStreamReturned = imageStream.ToArray();

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

                    var path = await SaveImageLocally(image, $"generated_image_{Path.GetRandomFileName()}.png");

                    
                    _validator.ClassifyImage(path);

                    return imageStreamReturned;
                }

                return null;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<string> SaveImageLocally(Image image, string fileName)
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

                return filePath;
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately (e.g., logging, error handling)
                Console.WriteLine($"Failed to save the image: {ex.Message}");
                throw;
            }
        }

        /*public async Task<List<string>> TypeControlNet()
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://127.0.0.1:7860/controlnet/model_list?update=true");

            var responseString = await response.Content.ReadAsStringAsync();

            // Deserialize the JSON string into ControlNetResponse using JsonConvert
            var controlNetResponse = JsonConvert.DeserializeObject<ControlNet>(responseString);

            // Return the list of strings from the deserialized object
            return controlNetResponse.model_list;
        }*/

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

        /// <summary>
        /// Image generation with Controlnet (QR model selected)
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="userImage"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<byte[]?> UseControlNet(string prompt, IAttachment userImage)
        {
            try
            {
                var imageUrl = userImage.Url;
                byte[] imageDownloaded;
                
                using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    imageDownloaded = imageBytes;
                }

                string base64String = Convert.ToBase64String(imageDownloaded);

                var controlNet = new ControlNet
                {
                    args = new List<ControlNetArgs>
                    {
                        new ControlNetArgs
                        {
                            input_image = base64String,
                            mask = null,
                            module = "none",
                            model = "controlnetQRPatternQR_v2Sd15 [2d8d5750]",
                            resize_mode = "Scale to Fit (Inner Fit)",            //Choose between: Just Resize, Scale to Fit (Inner Fit), Envelope (Outer Fit)
                            control_mode = "Balanced",                           //Chose between: Balanced, My prompt is more important, ControlNet is more important
                            weight = 1,
                            guidance_start = 0.1,
                            guidance_end = 0.9,
                            threshold_a = 50,
                            threshold_b = 150,
                            pixel_perfect = true,
                            lowvram = false,
                            processor_res = 64
                        }
                    }
                };

                var alwaysOnScripts = new alwayson_scripts
                {
                    ControlNet = controlNet
                };

                var payload = new Dictionary<string, object>
                {
                    { "prompt", prompt },
                    { "steps", 28 },
                    { "sampler_index", "Euler a" },
                    { "width", 600 },
                    { "height", 600 },
                    { "use_async", true },
                    { "cfg_scale", 7 },
                    { "alwayson_scripts", alwaysOnScripts }
                };

                using var httpclient = new HttpClient();
                string jsonPayload = JsonConvert.SerializeObject(payload);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await httpclient.PostAsync(txt2imgEndpoint, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

                foreach (var imageBase64 in GetImageList(responseObject["images"]))
                {
                    using var imageStream = new MemoryStream(Convert.FromBase64String(imageBase64.Split(",", 2)[0]));
                    var image = Image.FromStream(imageStream);

                    byte[] imageStreamReturned = imageStream.ToArray();

                    var path = await SaveImageLocally(image, $"generated_image_{Path.GetRandomFileName()}.png");

                    Console.WriteLine($"Image (ControlNet) Saved at: {path}");

                    return imageStreamReturned;
                }

                return null;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }


        public async Task<byte[]> Img2Img(string prompt, IAttachment userImage)
        {
            try
            {
                var imageUrl = userImage.Url;

                using var httpClient = new HttpClient();
                var imageDownloaded = await httpClient.GetByteArrayAsync(imageUrl);

                var base64String = Convert.ToBase64String(imageDownloaded);

                var images = new List<string> { base64String };
                
                var payload = new Dictionary<string, object>
                {
                    { "init_images", images},
                    { "denoising_strength", 0.6},
                    { "prompt", prompt },
                    { "steps", 28 },
                    { "sampler_index", _sampler },
                    { "width", 600 },
                    { "height", 600 },
                    { "negative_prompt", _negativePrompt},
                    { "cfg_scale", 7 }
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(img2imgEndpoint, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

                foreach (var imageBase64 in GetImageList(responseObject["images"]))
                {
                    using var imageStream = new MemoryStream(Convert.FromBase64String(imageBase64.Split(",", 2)[0]));
                    var image = Image.FromStream(imageStream);
                    byte[] imageStreamReturned = imageStream.ToArray();

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

                    var path = await SaveImageLocally(image, $"generated_image_{Path.GetRandomFileName()}.png");
                    Console.WriteLine($"Image (img2img) Saved at: {path}");

                    return imageStreamReturned;
                }

                throw new Exception("Unexpected error");

            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        
    }
}
