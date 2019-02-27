﻿using CarloSharp;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;

namespace SystemInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = Carlo.LaunchAsync(new Options()
            {
                BgColor = Color.FromArgb(0x2b, 0x2e, 0x3b),
                Title = "Systeminfo App",
                Width = 1000,
                Height = 500,
                Channel = new string[] { "canary", "stable" },
                Icon = "./app_icon.png",
            }).Result;

            app.ServeFolder("./www");

            app.ExposeFunctionAsync<JObject>("systeminfo", GetSystemInfo).Wait();

            app.Load("index.html");

            Console.ReadLine();
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
