using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using PuppeteerSharp;

namespace Carlo.Net
{
    public class App
    {
        private readonly Browser _browser;
        private readonly Options _options;
        private readonly Dictionary<Page, Window> _windows;
        private readonly Dictionary<string, object> _exposedFunctions;
        private readonly Dictionary<string, Tuple<Options, Action<Window>>> _pendingWindows;
        
        private int _windowSeq;
        private List<string> _www;
        private CDPSession _session;

        public App(Browser browser, Options options)
        {
            _browser = browser;
            _options = options;
            _windows = new Dictionary<Page, Window>();
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
                return _windows.Values.FirstOrDefault();
            }
        }

        internal async Task InitAsync()
        {
            //debugApp('Configuring browser');

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
                SetIcon(_options.Icon);
            }

            _browser.TargetCreated += OnBrowserTargetCreated;

            // Simulate the pageCreated sequence.
            _pendingWindows.Add("", new Tuple<Options, Action<Window>>(_options, null));

            await OnPageCreatedAsync(page);           
        }

        private async Task OnPageCreatedAsync(Page page)
        {
            var url = page.Url;
            //debugApp('Page created at', url);
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

            _windows.Add(page, window);

            callback?.Invoke(window);

            RaiseWindowCreatedEvent(window);
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

            await host.RunAsync();
        }

        public async Task ExposeFunctionAsync(string name, Action function)
        {
            _exposedFunctions.Add(name, function);

            var tasks = new List<Task>(_windows.Count);

            foreach (var window in _windows.Values)
            {
                tasks.Add(window.ExposeFunctionAsync(name, function));
            }

            await Task.WhenAll(tasks.ToArray());
        }

        public async Task ExposeFunctionAsync<T>(string name, Action<T> function)
        {
            _exposedFunctions.Add(name, function);

            var tasks = new List<Task>(_windows.Count);

            foreach (var window in _windows.Values)
            {
                tasks.Add(window.ExposeFunctionAsync(name, function));
            }

            await Task.WhenAll(tasks.ToArray());
        }

        public void SetIcon(string uri)
        {

        }

        public static IWebHostBuilder CreateWebHostBuilder(string folderPath)
        {
            return WebHost.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.Properties.Add("AppFolderPath", folderPath);      
                })
                .UseStartup<Startup>();
        }
    }
}
