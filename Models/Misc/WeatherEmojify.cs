using VergilBot.Models.Entities;

namespace VergilBot.Models.Misc
{
    public class WeatherEmojify
    {
        private string _description;
        private string _temperateure;
        private string descemoji;
        private string respondstringbuilder;
        private string _wind;
        private Forecast[] _forecast;


        public WeatherEmojify(Weather w, string city)
        {
            _description = w.Description;
            _temperateure = w.Temperature;
            _wind = w.Wind;
            _forecast = w.Forecast;
            descemoji = "";
            respondstringbuilder = $"Weather in {char.ToUpper(city[0]) + city.Substring(1)}:\n" +
                    $"🌡️ Temperature: {_temperateure},\n" +
                    $"🍃 Wind: {_wind},\n";
        }


        public string getEmojify()
        {
            if (_description.Contains("drizzle") && _description.Contains("fog"))
            {
                respondstringbuilder += $"🌧️ {_description}:foggy:";
                return respondstringbuilder;
            }

            if (_description.Equals("Clear"))
            {
                descemoji = "🌤️";
                if (DateTime.UtcNow.Hour > 18)
                {
                    descemoji = "🌕";
                }
                respondstringbuilder += $"{descemoji} {_description}";
                return respondstringbuilder;
            }

            if (_description.Contains("snow"))
            {
                respondstringbuilder += $":snowflake: {_description} :cloud_snow:";
                return respondstringbuilder;
            }

            if (_description.Equals("Partly cloudy"))
            {
                descemoji = "⛅";
            }
            else if (_description.Contains("Rain") || _description.Contains("rain") || _description.Contains("Drizzle") || _description.Contains("drizzle"))
            {
                descemoji = "🌧️";
            }
            else if (_description.Equals("Sunny"))
            {
                descemoji = "☀️";
            }

            respondstringbuilder += $"{descemoji} {_description}\n";
            return respondstringbuilder;
        }

    }
}
