using Carlo.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading;

namespace Photobooth
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = Carlo.Net.Carlo.LaunchAsync(new Options()
            {
                Title = "Photobooth",
                Width = 800,
                Height = 648 + 24,
                Channel = new string[] { "stable" }
            }).Result;

            var hostTask = app.ServeFolderAsync("./www");

            app.ExposeFunctionAsync<string, JObject>("saveImage", SaveImage).Wait();

            app.Load("index.html");

            hostTask.Wait();
        }

        private static JObject SaveImage(string base64)
        {
            Console.WriteLine(base64);

            return null;
        }
    }
}
