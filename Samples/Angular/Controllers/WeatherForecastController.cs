using CarloSharp.Samples.Angular.Model;
using System;
using System.Linq;

namespace CarloSharp.Samples.Angular.Controllers
{
    public class WeatherForecastController
    {
        class WeatherForecastArgs
        {
            public string City { get; set; }

            public bool UseCelsius { get; set; }
        }

        private static string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public WeatherForecastController(Window window)
        {
            window.IpcMessageReceived += OnIpcMessageReceived;
        }

        private void OnIpcMessageReceived(object sender, IpcMessageEventArgs e)
        {
            if (e.Channel == "getWeatherForecasts")
            {
                var args = e.Message.ToObject<WeatherForecastArgs>();

                e.Result = GetWeatherForecasts(args.City, args.UseCelsius);
            }
        }

        private WeatherForecast[] GetWeatherForecasts(string city, bool useCelsius)
        {
            var random = new Random();

            return Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    DateFormatted = DateTime.Now.AddDays(index).ToString("d"),
                    TemperatureC = random.Next(-20, 55),
                    Summary = Summaries[random.Next(Summaries.Length)]
                }).ToArray();
        }
    }
}
