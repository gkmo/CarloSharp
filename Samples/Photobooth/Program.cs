using CarloSharp;
using Newtonsoft.Json.Linq;
using System;

namespace Photobooth
{
    class Program
    {
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

            Console.ReadLine();
        }

        private static JObject SaveImage(string base64)
        {
            Console.WriteLine(base64);

            return null;
        }
    }
}
