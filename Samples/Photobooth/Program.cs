using CarloSharp;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;

namespace Photobooth
{
    class Program
    {
        private static ManualResetEvent _exitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var app = Carlo.LaunchAsync(new Options()
            {
                Title = "Photobooth",
                Width = 800,
                Height = 648 + 24,
                Channel = new string[] { "stable" }
            }).Result;

            app.ServeFolder("./www");

            app.ExposeFunctionAsync<string, JObject>("saveImage", SaveImage).Wait();

            app.Load("index.html");

            app.OnExit += OnAppExit;

            _exitEvent.WaitOne();
        }

        private static JObject SaveImage(string base64)
        {
            Console.WriteLine(base64);

            return null;
        }

        private static void OnAppExit(object sender, EventArgs args)
        {
            _exitEvent.Set();
        }
    }
}
