using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CarloSharp
{
    public class Window
    {
        private static Dictionary<string, string> _imageContentTypes = new Dictionary<string, string>()
        {
            {"jpeg", "image/jpeg"}, {"jpg", "image/jpeg"}, {"svg", "image/svg+xml"}, {"gif", "image/gif"}, {"webp", "image/webp"},
            {"png", "image/png"}, {"ico", "image/ico"}, {"tiff", "image/tiff"}, {"tif", "image/tiff"}, {"bmp", "image/bmp"}
        };

        private static Dictionary<string, string> _fontContentTypes = new Dictionary<string, string>()
        {
            {"ttf", "font/opentype"}, {"otf", "font/opentype"}, {"ttc", "font/opentype"}, {"woff", "application/font-woff"}
        };

        private readonly List<ServingItem> _www = new List<ServingItem>();
        private readonly App _app;
        private readonly Page _page;
        private Options _options;
        private CDPSession _session;
        private object _paramsForReuse;
        private int _windowId;
        private string _loadURI;
        private bool _interceptionInitialized;
        private RequestHandlerAsync _httpHandler;

        internal Window(App app, Page page, Options options)
        {
            _app = app;

            _page = page;
            _page.Close += OnCloseAsync;
            _page.DOMContentLoaded += OnDocumentLoadedAsync;

            _options = options;
        }

        public event EventHandler<IpcMessageEventArgs> IpcMessageReceived;

        public string LoadURI
        {
            get { return _loadURI; }
        }

        public RequestHandlerAsync HttpHandler
        {
            get { return _httpHandler; }

            set { _httpHandler = value; }
        }

        public async Task FullscreenAsync()
        {
            await SetWindowStateAsync("fullscreen");
        }

        public async Task MaximizeAsync()
        {
            await SetWindowStateAsync("maximized");
        }

        public async Task MinimizeAsync()
        {
            await SetWindowStateAsync("minimized");
        }

        public async Task CloseAsync()
        {
            await _page.CloseAsync();
        }

        public async Task ExposeFunctionAsync<TResult>(string name, Func<TResult> function)
        {
            await _page.ExposeFunctionAsync(name, function);
        }

        public async Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> function)
        {
            await _page.ExposeFunctionAsync(name, function);
        }

        public async Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> function)
        {
            await _page.ExposeFunctionAsync(name, function);
        }

        public async Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> function)
        {
            await _page.ExposeFunctionAsync(name, function);
        }

        public async Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> function)
        {
            await _page.ExposeFunctionAsync(name, function);
        }

        public async Task SendIpcMessageAsync(string channel, string message)
        {
            var script = $@"window.ipc.__receiveIpcMessage({_windowId}, '{channel}', '{message}');";

            await EvaluateAsync(script);
        }

        public async Task SendIpcMessageAsync(string channel, JObject obj)
        {
            await SendIpcMessageAsync(channel, obj.ToString());
        }

        internal async Task InitAsync()
        {
            var targetId = _page.Target.TargetId;

            _session = await _page.Target.CreateCDPSessionAsync();

            var args = new JObject
            {
                { "expression", new JValue("self.paramsForReuse") },
                { "returnByValue", new JValue(true) }
            };

            var getParamsForReuseTask = _session.SendAsync("Runtime.evaluate", args);

            var targetObject = $"{{ targetId: '{targetId}' }}";

            var getWindowForTargetTask = _app.Session.SendAsync("Browser.getWindowForTarget", JObject.Parse(targetObject));

            var color = new JObject
            {
                { "color", new JObject
                    {
                        { "r", new JValue(_options.BgColor.R) },
                        { "g", new JValue(_options.BgColor.G) },
                        { "b", new JValue(_options.BgColor.B) },
                        { "a", new JValue(_options.BgColor.A) },
                    }
                }
            };

            await _session.SendAsync("Emulation.setDefaultBackgroundColorOverride", color);

            var response = await getParamsForReuseTask;

            _paramsForReuse = response.Value<object>();

            var window = await getWindowForTargetTask;

            await InitBoundsAsync(window);
        }

        private async Task ConfigureRpcOnceAsync()
        {
            await ExposeFunctionAsync<int, string, JToken, JToken>("__sendIpcMessageAsync", ReceiveIpcMessageAsync);
            await ExposeFunctionAsync<int, string, JToken, JToken>("__sendIpcMessageSync", ReceiveIpcMessageSync);

            var assembly = typeof(Window).Assembly;
            var streamReader = new StreamReader(assembly.GetManifestResourceStream("CarloSharp.Scripts.Ipc.js"));

            var script = streamReader.ReadToEnd();

            streamReader.Close();

            script += $@"window.ipc = new Ipc({_windowId});";

            await EvaluateAsync(script);
        }

        private async Task InitBoundsAsync(JObject result)
        {
            _windowId = result.Value<int>("windowId");

            var bounds = new JObject
            {
                { "top", _options.Top.GetValueOrDefault() },
                { "left", _options.Left.GetValueOrDefault() },
                { "width", _options.Width ?? 800 },
                { "height", _options.Height ?? 600 }
            };

            await SetBoundsAsync(bounds);
        }

        private async void OnDocumentLoadedAsync(object sender, EventArgs e)
        {
            await ConfigureRpcOnceAsync();

            var pingUiThread = new Thread(async () => 
            {
                try
                {
                    while (true)
                    {
                        await EvaluateAsync("window.__lastBackendPing = new Date().getTime();");
                        Thread.Sleep(500);
                    }
                }
                catch (PuppeteerException ex)
                {
                    _app.DebugServer(ex.Message);
                }
            });

            pingUiThread.IsBackground = true;
            pingUiThread.Start();

            var script = @"
                var checkBackendProcess = function() {
                    if (window.__lastBackendPing < new Date().getTime() - 1000) {
                        window.close(); 
                    }
                };

                setInterval(checkBackendProcess, 1000);";

            await EvaluateAsync(script);
        }

        private JToken ReceiveIpcMessageAsync(int windowId, string channel, JToken message)
        {
            _app.DebugServer("Received Async Ipc Message: " + message + " on channel :" + channel);

            Task.Run(() =>
            {
                IpcMessageReceived?.Invoke(this, new IpcMessageEventArgs(channel, message));
            });

            return null;
        }

        private JToken ReceiveIpcMessageSync(int windowId, string channel, JToken message)
        {
            _app.DebugServer("Received Sync Ipc Message: " + message + " on channel :" + channel);

            var e = new IpcMessageEventArgs(channel, message);

            IpcMessageReceived?.Invoke(this, e);

            return JToken.FromObject(e.Result);
        }

        private async void OnCloseAsync(object sender, EventArgs e)
        {
            await _app.WindownClosedAsync(this);
        }

        public async Task LoadAsync(string uri, Options options)
        {
            _app.DebugApp("Load page {0}", uri);

            _loadURI = uri;
            _options = options;

            await InitializeInterception();

            _app.DebugApp("Navigating the page to {0}", _loadURI);

            var navigationOptions = new NavigationOptions()
            {
                Timeout = 0,
                WaitUntil = new WaitUntilNavigation[] { WaitUntilNavigation.DOMContentLoaded }
            };

            if (_loadURI.StartsWith("http://") || _loadURI.StartsWith("https://"))
            {
                await _page.GoToAsync(_loadURI, navigationOptions);
            }
            else
            {
                await _page.GoToAsync("https://domain/" + _loadURI, navigationOptions);
            }

            // Available in Chrome M73+.
            try
            {
                await _session.SendAsync("Page.resetNavigationHistory");
            }
            catch { }
        }

        private async Task InitializeInterception()
        {
            _app.DebugApp("Initializing network interception...");

            if (_interceptionInitialized)
            {
                return;
            }

            if (this._www.Count + _app.WWW.Count == 0 && _httpHandler == null && _app.HttpHandler == null)
            {
                return;
            }

            _interceptionInitialized = true;

            _session.MessageReceived += (sender, args) => 
            {
                if (args.MessageID == "Network.requestIntercepted")
                {
                    RequestInterceptedAsync(args.MessageData);
                }
            };

            await _session.SendAsync("Network.setRequestInterception", JObject.Parse("{patterns: [{urlPattern: '*'}]}"));
        }

        public async Task<JToken> EvaluateAsync(string script)
        {
            return await _page.EvaluateExpressionAsync(script);
        }

        private async void RequestInterceptedAsync(JToken messageData)
        {
            var url = messageData["request"]["url"].Value<string>();

            _app.DebugServer("intercepted: {0}", url);

            var handlers = new Queue<RequestHandlerAsync>();

            if (_httpHandler != null)
            {
                handlers.Enqueue(_httpHandler);
            }

            if (_app.HttpHandler != null)
            {
                handlers.Enqueue(_app.HttpHandler);
            }

            handlers.Enqueue(HandleRequestAsync);

            var request = new HttpRequest(_session, messageData, handlers);

            await request.CallNextHandlerAsync();
        }

        private async Task SetWindowStateAsync(string state)
        {
            var bounds = new JObject
            {
                { "windowState", state }
            };

            await SetBoundsAsync(bounds);
        }

        private async Task SetBoundsAsync(JObject bounds)
        {
            var args = new JObject
            {
                { "windowId", _windowId },
                { "bounds", bounds }
            };

            await _app.Session.SendAsync("Browser.setWindowBounds", args);
        }   

        private async Task HandleRequestAsync(HttpRequest request) 
        {
            var url = new Uri(request.Url);

            _app.DebugServer("request url: {0}", url.ToString());

            if (url.Host != "domain") 
            {
                await request.DeferToBrowserAsync();
                return;
            }

            var urlpathname = url.LocalPath;

            var www = new List<ServingItem>(_app.WWW);
            www.AddRange(_www);
            
            foreach (var entry in www) 
            {
                var prefix = entry.Prefix;

                _app.DebugServer("prefix: {0}", prefix);

                if (!urlpathname.StartsWith(prefix))
                {
                    continue;
                }

                var pathname = urlpathname.Substring(prefix.Length);

                _app.DebugServer("pathname: {0}", pathname);

                if (entry.BaseUrl != null) 
                {
                    //TODO: request.DeferToBrowser({ url: String(new URL(pathname, baseURL)) });
                    return;
                }
                
                var fileName = Path.Combine(entry.Folder, pathname);

                if (!File.Exists(fileName))
                {
                    continue;
                }

                var headers = new Dictionary<string, string>() 
                { 
                    {"content-type", ContentType(request, fileName)} 
                };
                
                var body = File.ReadAllBytes(fileName);

                await request.FulfillAsync(null, headers, body);
                
                return;
            }

            await request.DeferToBrowserAsync();
        }

        private static string ContentType(HttpRequest request, string fileName)
        {
            var dotIndex = fileName.LastIndexOf(".");
            var extension = fileName.Substring(dotIndex + 1);

            switch (request.ResourceType) 
            {
                case "Document":
                    return "text/html";
                case "Script":
                    return "text/javascript";
                case "Stylesheet":
                    return "text/css";
                case "Image":
                    return _imageContentTypes.ContainsKey(extension) ? _imageContentTypes[extension] : "image/png";
                case "Font":
                    return _fontContentTypes.ContainsKey(extension) ? _fontContentTypes[extension] : "application/font-woff";
            }

            return null;
        }
    }
}