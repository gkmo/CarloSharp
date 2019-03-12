using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly List<ServingItem> _www = new List<ServingItem>();

        private int _windowSeq;
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
        }

        public event EventHandler Exit;
        public event EventHandler<WindowEventArgs> WindowCreated;

        internal CDPSession Session {  get { return _session; } }

        internal IList<ServingItem> WWW { get { return _www.AsReadOnly(); } }

        public Window MainWindow
        {
            get
            {
                return _windows.FirstOrDefault();
            }
        }

        public RequestHandlerAsync HttpHandler { get; set; }

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

            DebugApp("Page created at {0}", url);

            var seq = url.StartsWith("about:blank?seq=") ? url.Substring("about:blank?seq=".Length) : "";

            if (!_pendingWindows.TryGetValue(seq, out var p))
            {
                return;
            }

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
            DebugApp("Window closed {0}", window.LoadURI);

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

            Exit?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseWindowCreatedEvent(Window window)
        {
            WindowCreated?.Invoke(this, new WindowEventArgs(window));
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

        public Window CreateWindow(Options options = null)
        {
            if (_windows.Count == 0)
            {
                throw new Exception("Needs at least one window to create more.");
            }

            if (options == null)
            {
                options = _options.Clone();
            }

            var @params = new List<string>();

            if (options.Width.HasValue)
            {
                @params.Add($"width={options.Width.Value}");
            }

            if (options.Height.HasValue)
            {
                @params.Add($"height={options.Height.Value}");
            }

            if (options.Left.HasValue)
            {
                @params.Add($"left={options.Left.Value}");
            }

            if (options.Top.HasValue)
            {
                @params.Add($"top={options.Top.Value}");
            }

            var seq = ++_windowSeq;

            Window newWindow = null;
            ManualResetEvent windowCreatedEvent = new ManualResetEvent(false);
            var callback = new Action<Window>(w => { newWindow = w; windowCreatedEvent.Set(); });

            _pendingWindows[seq.ToString()] = new Tuple<Options, Action<Window>>(options, callback);

            var args = string.Join(", ", @params);
            var script = $"window.open('about:blank?seq={seq}', '', '{args}');";

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            MainWindow.EvaluateAsync(script);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            windowCreatedEvent.WaitOne();

            return newWindow;
        }
        
        public async Task LoadAsync(string uri, Options options = null)
        {
            await MainWindow.LoadAsync(uri, options);
        }

        public void ServeFolder(string folderPath = "", string prefix = "")
        {
            _www.Add(new ServingItem() { Prefix = WrapPrefix(prefix), Folder = folderPath });
        }

        public void ServeOrigin(string baseUrl = "", string prefix = "")
        {
            _www.Add(new ServingItem() { Prefix = WrapPrefix(prefix), BaseUrl = baseUrl });
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

        internal void DebugApp(string message, params string[] args)
        {
            Carlo.Logger?.Debug(message, args);
        }

        internal void DebugServer(string message, params string[] args)
        {
            Carlo.Logger?.Debug(message, args);
        }

        internal static string WrapPrefix(string prefix) 
        {
            if (!prefix.StartsWith("/")) 
            {
                prefix = '/' + prefix;
            }

            if (!prefix.EndsWith("/")) 
            {
                prefix += '/';
            }
            
            return prefix;
        }
    }
}
