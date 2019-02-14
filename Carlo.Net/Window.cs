using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.Threading.Tasks;

namespace Carlo.Net
{
    public class Window
    {
        private readonly App _app;
        private readonly Page _page;

        private Options _options;
        private CDPSession _session;
        private object _paramsForReuse;
        private int _windowId;
        private string _loadURI;

        internal Window(App app, Page page, Options options)
        {
            _app = app;

            _page = page;
            _page.Close += OnClose;
            _page.DOMContentLoaded += OnDocumentLoaded;

            _options = options;
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

            ConfigureRpcOnce();
        }


        /* 
    
    await Promise.all([
     
      this.configureRpcOnce_(),
      ...this.app_.exposedFunctions_.map(({name, func}) => this.exposeFunction(name, func))
    ]);
             */

        private void ConfigureRpcOnce()
        {

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

        private async Task SetBoundsAsync(JObject bounds)
        {
            var args = new JObject
            {
                { "windowId", _windowId },
                { "bounds", bounds }
            };

            await _app.Session.SendAsync("Browser.setWindowBounds", args);
        }

        private void OnDocumentLoaded(object sender, EventArgs e)
        {

        }

        private void OnClose(object sender, EventArgs e)
        {

        }

        internal async void LoadAsync(string uri, Options options)
        {
            //debugApp('Load page', uri);

            _loadURI = uri;
            _options = options;

            await InitializeInterception();

            //debugApp('Navigating the page to', this.loadURI_);

            //const result = new Promise(f => this.domContentLoadedCallback_ = f);
            // Await here to process exceptions.

            var navigationOptions = new NavigationOptions()
            {
                Timeout = 0,
                WaitUntil = new WaitUntilNavigation[] { WaitUntilNavigation.DOMContentLoaded }
            };

            await _page.GoToAsync("http://localhost:5000/" + _loadURI, navigationOptions);

            // Available in Chrome M73+.
            try
            {
                await _session.SendAsync("Page.resetNavigationHistory");
            }
            catch { }
        }


        private async Task InitializeInterception()
        {
            //throw new NotImplementedException();
        }

        public async Task ExposeFunctionAsync(string name, Action function)
        {
            await _page.ExposeFunctionAsync(name, function);
        }

        public async Task ExposeFunctionAsync<T>(string name, Action<T> action)
        {
            var function = new Func<T, object>((obj) =>
            {
                action(obj);
                return null;
            });

            await _page.ExposeFunctionAsync(name, function);
        }

        public async Task ExposeFunctionAsync<TResult>(string name, Func<TResult> function)
        {
            await _page.ExposeFunctionAsync(name, function);
        }
    }
}