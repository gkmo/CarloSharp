using Carlo.Net;
using System;
using System.Diagnostics;
using System.Threading;

namespace Photobooth
{
    class Program
    {
        static ManualResetEvent waitAppExit = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var app = Carlo.Net.Carlo.LaunchAsync(new Options()
            {
                Title = "Photobooth",
                Width = 800,
                Height = 648 + 24,
                Channel = new string[] { "stable" }
            }).Result;

            app.OnExit += App_OnExit;

            var hostTask = app.ServeFolderAsync("./www");

            app.ExposeFunctionAsync<string>("saveImage", SaveImage).Wait();

            app.Load("index.html");

            hostTask.Wait();
        }

        private static void App_OnExit(object sender, EventArgs e)
        {
            waitAppExit.Set();
        }

        private static void SaveImage(string base64)
        {
            Console.WriteLine(base64);
        }
    }
}
