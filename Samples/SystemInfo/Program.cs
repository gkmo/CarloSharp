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
            var app = Carlo.Launch(new Options()
            {
                BgColor = Color.FromArgb(0x2b, 0x2e, 0x3b),
                Title = "Carlo# - Systeminfo App",
                Width = 1000,
                Height = 500,
                Channel = new string[] { "canary", "stable" },
                Icon = "./app_icon.png",
            });

            app.ServeFolder("./www");

            app.ExposeFunctionAsync("systeminfo", GetSystemInfo).Wait();

            app.Load("index.html");

            app.Exit += OnAppExit;

            _exitEvent.WaitOne();
        }

        private static JObject GetSystemInfo()
        {
            var result = new JObject();

            result["cpu"] = new JObject
            {
                { "cores", Environment.ProcessorCount }
            };

            result["osInfo"] = new JObject
            {
                { "platform", Environment.OSVersion.Platform.ToString() },
                { "version", Environment.OSVersion.VersionString },
                { "64 bits", Environment.Is64BitOperatingSystem }
            };

            result[".net"] = new JObject
            {
                { "version", Environment.Version.ToString() }
            };

            return result;
        }

        private static void OnAppExit(object sender, EventArgs args)
        {
            _exitEvent.Set();
        }
    }
}
