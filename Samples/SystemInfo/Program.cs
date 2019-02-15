using Carlo.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;

namespace SystemInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = Carlo.Net.Carlo.LaunchAsync(new Options()
            {
                BgColor = Color.FromArgb(0x2b, 0x2e, 0x3b),
                Title = "Systeminfo App",
                Width = 1000,
                Height = 500,
                Channel = new string[] { "canary", "stable" },
                Icon = "./app_icon.png",
            }).Result;

            var hostTask = app.ServeFolderAsync("./www");

            app.ExposeFunctionAsync<JObject>("systeminfo", GetSystemInfo).Wait();

            app.Load("index.html");

            hostTask.Wait();
        }

        private static JObject GetSystemInfo()
        {
            var result = new JObject()
            {
                { "battery", "" },
                { "cpu", ""},
                { "osInfo", JObject.FromObject(Environment.OSVersion) }
            };

            return result;
        }
    }
}
