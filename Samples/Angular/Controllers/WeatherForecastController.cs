using CarloSharp.Samples.Angular.Model;
using System;
using System.Linq;

namespace CarloSharp.Samples.Angular.Controllers
{
    public class WeatherForecastController
    {
        private static string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public WeatherForecastController(Window window)
        {
            window.ExposeFunctionAsync("getWeatherForecasts", GetWeatherForecasts).Wait();
        }

        public WeatherForecast[] GetWeatherForecasts()
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
