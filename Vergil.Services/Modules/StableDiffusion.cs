using System.Drawing;
using System.Text;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vergil.Services.Misc;
using Image = System.Drawing.Image;

namespace Vergil.Services.Modules;

public interface IStableDiffusion
{
    Task<byte[]?> GenerateImage(string prompt, int? width, int? height, IUser user);
    Task<byte[]?> UseControlNet(string prompt, IAttachment userImage, IUser user);
    Task<byte[]?> Img2Img(string prompt, IAttachment userImage, IUser user);
    Task<bool> IsApiAvailable();
}
    
public class StableDiffusion : IStableDiffusion
{
    private readonly string _sampler;
    private readonly string _negativePrompt;
    private readonly string infoEndpoint;
    private readonly string txt2imgEndpoint;
    private readonly string img2imgEndpoint;
    private readonly IStableDiffusionValidator _validator;

    public StableDiffusion(IStableDiffusionValidator diffusionValidator)
    {
        var url = "http://127.0.0.1:7860";
        _sampler = "Euler a";
        _negativePrompt = "nsfw, lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, " +
                          "fewer digits, cropped, worst quality, low quality, normal quality, jpeg artifacts, " +
                          "signature, watermark, username, blurry, artist name, rating: nsfw";
        infoEndpoint = $"{url}/sdapi/v1/png-info";
        txt2imgEndpoint = $"{url}/sdapi/v1/txt2img";
        img2imgEndpoint = $"{url}/sdapi/v1/img2img";
        _validator = diffusionValidator;
    }
    public async Task<byte[]?> GenerateImage(string prompt, int? width, int? height, IUser user)
    {
        try
        {
            var payload = new Dictionary<string, object>
            {
                { "prompt", $"{prompt}, rating: general, masterpiece, best quality" },
                { "steps", 28 },
                { "sampler_index", _sampler},
                { "width", width ?? 832},
                { "height", height ?? 1216},
                { "negative_prompt", _negativePrompt},
                { "cfg_scale", 6},
                { "do_not_save_samples", true},
                { "do_not_save_grid", true}
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

                var path = await SaveImageLocally(image, $"generated_image_{Path.GetRandomFileName()}.png", user);
                Console.WriteLine($"(generation) Image Saved at: {path}");

                
                //_validator.ClassifyImage(path);

                return imageStreamReturned;
            }

            return null;

        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private async Task<string> SaveImageLocally(Image image, string fileName, IUser user)
    {
        try
        {
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"GeneratedImages\{user.Username}");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);


            ImageConverter converter = new ImageConverter();
            byte[] imageBytes = (byte[])converter.ConvertTo(image, typeof(byte[]));

            await File.WriteAllBytesAsync(filePath, imageBytes);

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
    public async Task<byte[]?> UseControlNet(string prompt, IAttachment userImage, IUser user)
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

            var alwaysOnScripts = new AlwaysOnScripts()
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

                var path = await SaveImageLocally(image, $"generated_image_{Path.GetRandomFileName()}.png", user);

                Console.WriteLine($"(ControlNet) Image Saved at: {path}");

                return imageStreamReturned;
            }

            return null;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }


    public async Task<byte[]?> Img2Img(string prompt, IAttachment userImage, IUser user)
    {
        try
        {
            var imageUrl = userImage.Url;
            var imageHeight = userImage.Height;
            var imageWidth = userImage.Width;

            if (imageHeight > 1000 || imageWidth > 1000)
            {
                if (imageHeight > imageWidth)
                {
                    imageHeight = 1152;
                    imageWidth = 896;
                }
                else if (imageHeight < imageWidth)
                {
                    imageHeight = 832;
                    imageWidth = 1216;
                }
                else
                {
                    imageHeight = 1024;
                    imageWidth = 1024;
                }
            }

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
                { "width", imageWidth },
                { "height", imageHeight },
                { "negative_prompt", _negativePrompt},
                { "cfg_scale", 6 },
                { "resize_mode", 2}
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

                var path = await SaveImageLocally(image, $"generated_image_{Path.GetRandomFileName()}.png", user);
                Console.WriteLine($"(img2img) Image Saved at: {path}");

                return imageStreamReturned;
            }

            throw new Exception("Unexpected error");

        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public async Task<bool> IsApiAvailable()
    {
        HttpClient client = new HttpClient();
        var progressResponse = await client.GetAsync("http://127.0.0.1:7860/sdapi/v1/progress?skip_current_image=false");
        var progress = await progressResponse.Content.ReadAsStringAsync();
        dynamic data = JsonConvert.DeserializeObject(progress);

        if (data.state.job_count == 0)
        {
            return true;
        }

        return false;
    }
    
}