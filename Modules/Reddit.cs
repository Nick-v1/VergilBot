using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using VergilBot.Models.Misc;

namespace VergilBot.Modules
{
    public class Reddit
    {
        private string url;
        private IUser botUser;

        public Reddit(string subreddit, IUser botuser)
        {
            this.url = $"https://www.reddit.com/r/{subreddit}/new.json?limit=1000";
            this.botUser = botuser ;
        }

        public async Task<EmbedBuilder> sendRequest()
        {
            var embed = new EmbedBuilder();
            using (var httpClient = new HttpClient()) 
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                var response = await httpClient.GetStringAsync(url);

                JObject obj = JObject.Parse(response);

                JArray posts = (JArray)obj["data"]["children"];

                /*Random random = new Random();
                JToken post = posts[random.Next(posts.Count)];*/

                var random = ThreadLocalRandom.Next(0, posts.Count);
                JToken post = posts[random];

                // Get the title and author of the post
                string title = post["data"]["title"].ToString();
                string author = post["data"]["author"].ToString();
                string subreddit_name_prefixed = post["data"]["subreddit_name_prefixed"].ToString();
                string permalink = post["data"]["permalink"].ToString();
                string redditPostUrl = $"https://www.reddit.com/{permalink}";
                string bodyText = post["data"]["selftext"].ToString();


                // Image checking
                string imageUrl = null;
                if (post["data"]["url"].ToString().EndsWith(".jpg") ||
                    post["data"]["url"].ToString().EndsWith(".jpeg") ||
                    post["data"]["url"].ToString().EndsWith(".png"))
                {
                    imageUrl = post["data"]["url"].ToString();
                }


                embed
                    .WithTitle(title)
                    .WithDescription($"{post["data"]["url_overridden_by_dest"]}")
                    .WithColor(Color.Orange)
                    .WithFooter($"from: {subreddit_name_prefixed} by {author}\n" +
                    $"{botUser.Username}#{botUser.Discriminator}", botUser.GetAvatarUrl())
                    .WithCurrentTimestamp()
                    .WithUrl(redditPostUrl);

                if (imageUrl != null)
                {
                    embed.WithImageUrl(imageUrl);
                }

                if (!string.IsNullOrEmpty(bodyText))
                    embed.WithDescription(bodyText);


            }
            return embed;
        }
        
        

    }
}