namespace Vergil.Services.Misc;
using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public partial class Weather
    {
        [JsonProperty("temperature")]
        public string Temperature { get; set; }

        [JsonProperty("wind")]
        public string Wind { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("forecast")]
        public Forecast[] Forecast { get; set; }
    }

    public partial class Forecast
    {
        [JsonProperty("day")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Day { get; set; }

        [JsonProperty("temperature")]
        public string Temperature { get; set; }

        [JsonProperty("wind")]
        public string Wind { get; set; }
    }

    public partial class Weather
    {
        public static Weather? FromJson(string json) => JsonConvert.DeserializeObject<Weather>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Weather self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings? Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object? ReadJson(JsonReader reader, Type t, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object? untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }