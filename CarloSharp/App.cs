using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace CarloSharp
{
    public class App
    {
        private readonly Browser _browser;
        private readonly Options _options;
        private readonly List<Window> _windows;
        private readonly Dictionary<string, object> _exposedFunctions;
        private readonly Dictionary<string, Tuple<Options, Action<Window>>> _pendingWindows;
        private readonly List<IWebHost> _hosts = new List<IWebHost>();

        private int _windowSeq;
        private List<string> _www;
        private CDPSession _session;
        private bool _exited;

        public App(Browser browser, Options options)
        {
            _browser = browser;
            _options = options;
            _windows = new List<Window>();
            _exposedFunctions = new Dictionary<string, object>();
            _pendingWindows = new Dictionary<string, Tuple<Options, Action<Window>>>();
            _windowSeq = 0;
            _www = new List<string>();
        }

        public event EventHandler OnExit;
        public event EventHandler<WindowEventArgs> OnWindowCreated;

        internal CDPSession Session {  get { return _session; } }

        public Window MainWindow
        {
            get
            {
                return _windows.FirstOrDefault();
            }
        }

        internal async Task InitAsync()
        {
            DebugApp("Configuring browser");

            var createSessionTask = _browser.Target.CreateCDPSessionAsync();

            var overridePermissionsTask =_browser.DefaultContext.OverridePermissionsAsync("https://domain", 
                new OverridePermission[] {
                    OverridePermission.Geolocation,
                    OverridePermission.Midi,
                    OverridePermission.Notifications,
                    OverridePermission.Camera,
                    OverridePermission.Microphone,
                    OverridePermission.ClipboardRead,
                    OverridePermission.ClipboardWrite });

            var pagesTask = _browser.PagesAsync();

            _session = await createSessionTask;

            var page = (await pagesTask)[0];

            if (_options.Icon != null)
            {
                await SetIconAsync(_options.Icon);
            }

            _browser.TargetCreated += OnBrowserTargetCreated;

            // Simulate the pageCreated sequence.
            _pendingWindows.Add("", new Tuple<Options, Action<Window>>(_options, null));

            await OnPageCreatedAsync(page);           
        }

        private async Task OnPageCreatedAsync(Page page)
        {
            var url = page.Url;

            DebugApp("Page created at", url);

            var seq = url.StartsWith("about:blank?seq=") ? url.Substring(0, "about:blank?seq=".Length) : "";
            var p = _pendingWindows[seq];
            var options = p.Item1;
            var callback = p.Item2;

            if (options == null)
            {
                options = _options;
            }
            
            _pendingWindows.Remove(seq);

            var window = new Window(this, page, options);

            await window.InitAsync();

            _windows.Add(window);

            callback?.Invoke(window);

            RaiseWindowCreatedEvent(window);
        }

        internal async Task WindownClosedAsync(Window window)
        {
            DebugApp("Window closed", window.LoadURI);

            _windows.Remove(window);

            if (_windows.Count == 0)
            {
                await ExitAsync();
            }
        }

        public async Task ExitAsync()
        {
            DebugApp("App.exit...");

            if (_exited)
            {
                return;
            }

            _exited = true;

            await _browser.CloseAsync();

            foreach (var host in _hosts)
            {
                await host.StopAsync();
            }

            OnExit?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseWindowCreatedEvent(Window window)
        {
            OnWindowCreated?.Invoke(this, new WindowEventArgs(window));
        }

        private async void OnBrowserTargetCreated(object sender, TargetChangedArgs e)
        {
            var page = await e.Target.PageAsync();

            if (page == null)
            {
                return;
            }

            await OnPageCreatedAsync(page);
        }

        public Window CreateWindow()
        {
            return null;
        }

        public void Load(string uri, Options options = null)
        {
            MainWindow.LoadAsync(uri, options);
        }

        public async Task ServeFolderAsync(string folderPath)
        {
            var host = CreateWebHostBuilder(folderPath).Build();

            _hosts.Add(host);

            await host.RunAsync();
        }

        public async Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> function)
        {
            _exposedFunctions.Add(name, function);

            var tasks = new List<Task>(_windows.Count);

            foreach (var window in _windows)
            {
                tasks.Add(window.ExposeFunctionAsync(name, function));
            }

            await Task.WhenAll(tasks.ToArray());
        }

        public async Task ExposeFunctionAsync<T>(string name, Func<T> function)
        {
            _exposedFunctions.Add(name, function);

            var tasks = new List<Task>(_windows.Count);

            foreach (var window in _windows)
            {
                tasks.Add(window.ExposeFunctionAsync(name, function));
            }

            await Task.WhenAll(tasks.ToArray());
        }

        public async Task SetIconAsync(string icon)
        {
            var buffer = File.ReadAllBytes(icon);

            var iconObject = new JObject()
            {
                { "image", Convert.ToBase64String(buffer) }
            };

            await _session.SendAsync("Browser.setDockTile", iconObject);       
        }

        public static IWebHostBuilder CreateWebHostBuilder(string folderPath)
        {
            return WebHost.CreateDefaultBuilder()
                .UseUrls("http://*:5000;http://localhost:5001;https://hostname:5002")
                .ConfigureServices(s => { s.AddSpaStaticFiles(c => c.RootPath = folderPath); })
                .Configure(app =>
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();

                    app.UseStaticFiles();
                    app.UseSpaStaticFiles();

                    app.UseSpa(spa =>
                    {
                        spa.Options.SourcePath = folderPath;
                    });
                });
        }

        internal void DebugApp(string message, params string[] args)
        {
            Console.WriteLine(message, args);
        }
    }
}
