using CarloSharp;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Photobooth
{
    class Program
    {
        private static ManualResetEvent _exitEvent = new ManualResetEvent(false);
        private static App _app;

        static void Main(string[] args)
        {
            _app = Carlo.LaunchAsync(new Options()
            {
                Title = "Photobooth",
                Width = 800,
                Height = 648 + 24,
                Channel = new string[] { "canary" }
            }).Result;

            _app.ServeFolder("./www");

            _app.ExposeFunctionAsync<string, JObject>("saveImage", SaveImage).Wait();

            _app.LoadAsync("index.html").Wait();

            _app.Exit += OnAppExit;

            _exitEvent.WaitOne();
        }

        private static JObject SaveImage(string base64)
        {
            var buffer = Convert.FromBase64String(base64);
            
            if (!Directory.Exists("pictures"))
            {
                Directory.CreateDirectory("pictures");
            }
  
            var fileName = Path.Combine("pictures", DateTime.Now.ToFileTime() + ".jpeg");
            
            File.WriteAllBytes(fileName, buffer);

            return null;
        }

        private static void OnAppExit(object sender, EventArgs args)
        {
            _exitEvent.Set();
        }
    }
}
