using System;
using System.Threading;

namespace CarloSharp.Samples.HelloWorld
{
    class Program
    {
        private static ManualResetEvent _exitEvent = new ManualResetEvent(false);
        
        static void Main(string[] args)
        {
            var app = Carlo.Launch(new Options());

            app.ServeFolder("./wwwroot");

            app.Load("index.html");

            app.Exit += OnAppExit;

            var thread = new Thread((s) =>
            {
                while (true)
                {
                    app.MainWindow?.SendIpcMessageAsync("message-from-csharp", DateTime.Now.ToString());

                    Thread.Sleep(1000);
                }
            });

            thread.IsBackground = true;
            thread.Start();

            _exitEvent.WaitOne();
        }

        private static void OnAppExit(object sender, EventArgs args)
        {
            _exitEvent.Set();
        }
    }
}
