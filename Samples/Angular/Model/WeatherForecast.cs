using Newtonsoft.Json;

namespace CarloSharp.Samples.Angular.Model
{
    public class WeatherForecast
    {
        [JsonProperty("dateFormatted")]
        public string DateFormatted { get; set; }

        [JsonProperty("temperatureC")]
        public int TemperatureC { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("temperatureF")]
        public int TemperatureF
        {
            get
            {
                return 32 + (int)(TemperatureC / 0.5556);
            }
        }
    }
}
