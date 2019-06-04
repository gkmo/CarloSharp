
using CarloSharp.Samples.Angular.Controllers;
using System;
using System.Threading;

namespace CarloSharp.Samples.Angular
{
    public class Program
    {
        private static ManualResetEvent _exitEvent = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
            var app = Carlo.Launch(new Options()
            {
                Title = "Carlo# - Angular",
                Width = 1024,
                Height = 600,
                Channel = new string[] { "*" }
            });

            var controller = new WeatherForecastController(app.MainWindow);

            app.ServeFolder("./wwwroot/dist");

            app.Load("index.html");

            app.Exit += OnAppExit;

            _exitEvent.WaitOne();
        }

        private static void OnAppExit(object sender, EventArgs args)
        {
            _exitEvent.Set();
        }
    }
}
