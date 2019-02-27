using CarloSharp.Samples.Angular.Controllers;
using CarloSharp;
using System;

namespace CarloSharp.Samples.Angular
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = Carlo.LaunchAsync(new Options()
            {
                Title = "Carlo# - Angular",
                Width = 1024,
                Height = 600,
                Channel = new string[] { "stable" }
            }).Result;

            var controller = new WeatherForecastController(app.MainWindow);

            app.ServeFolder("./wwwroot/dist");

            app.Load("index.html");

            Console.ReadLine();
        }
    }
}
