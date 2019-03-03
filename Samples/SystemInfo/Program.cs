using CarloSharp;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Threading;

namespace SystemInfo
{
    class Program
    {
        private static ManualResetEvent _exitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var app = Carlo.LaunchAsync(new Options()
            {
                BgColor = Color.FromArgb(0x2b, 0x2e, 0x3b),
                Title = "Carlo# - Systeminfo App",
                Width = 1000,
                Height = 500,
                Channel = new string[] { "canary", "stable" },
                Icon = "./app_icon.png",
            }).Result;

            app.ServeFolder("./www");

            app.ExposeFunctionAsync<JObject>("systeminfo", GetSystemInfo).Wait();

            app.LoadAsync("index.html").Wait();

            app.OnExit += OnAppExit;

            _exitEvent.WaitOne();
        }

        private static JObject GetSystemInfo()
        {
            var result = new JObject()
            {
                { "battery", "" },
                { "cpu", ""},
                { "osInfo", Environment.OSVersion.ToString() }
            };

            return result;
        }

        private static void OnAppExit(object sender, EventArgs args)
        {
            _exitEvent.Set();
        }
    }
}
