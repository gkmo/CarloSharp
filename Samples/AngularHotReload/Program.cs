using CarloSharp;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;

namespace CarloSharp.Samples.Angular.HotReload
{
    public class Program
    {
        private static IWebHost _host;

        public static void Main(string[] args)
        {
            _host = CreateWebHostBuilder(args).Build();

            var runTask = _host.RunAsync();

            var app = Carlo.LaunchAsync(new Options()
            {
                Title = "Carlo# - Angular with hot reload",
                Width = 1024,
                Height = 600,
                Channel = new string[] { "stable" }
            }).Result;

            app.Exit += App_OnExit;

            app.ServeOrigin("https://localhost:5001");

            app.LoadAsync("index.html").Wait();

            runTask.Wait();
        }

        private static void App_OnExit(object sender, EventArgs e)
        {
            _host.StopAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
