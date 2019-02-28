
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace CarloSharp 
{
    public delegate Task RequestHandlerAsync(HttpRequest request);

    public class HttpRequest 
    {
        private readonly Dictionary<string, string> _statusTexts = new Dictionary<string, string>()
        {
            {"100", "Continue" },
            {"101", "Switching Protocols"},
            {"102", "Processing"},
            {"200", "OK"},
            {"201", "Created"},
            {"202", "Accepted"},
            {"203", "Non-Authoritative Information"},
            {"204", "No Content"},
            {"206", "Partial Content"},
            {"207", "Multi-Status"},
            {"208", "Already Reported"},
            {"209", "IM Used"},
            {"300", "Multiple Choices"},
            {"301", "Moved Permanently"},
            {"302", "Found"},
            {"303", "See Other"},
            {"304", "Not Modified"},
            {"305", "Use Proxy"},
            {"306", "Switch Proxy"},
            {"307", "Temporary Redirect"},
            {"308", "Permanent Redirect"},
            {"400", "Bad Request"},
            {"401", "Unauthorized"},
            {"402", "Payment Required"},
            {"403", "Forbidden"},
            {"404", "Not Found"},
            {"405", "Method Not Allowed"},
            {"406", "Not Acceptable"},
            {"407", "Proxy Authentication Required"},
            {"408", "Request Timeout"},
            {"409", "Conflict"},
            {"410", "Gone"},
            {"411", "Length Required"},
            {"412", "Precondition Failed"},
            {"413", "Payload Too Large"},
            {"414", "URI Too Long"},
            {"415", "Unsupported Media Type"},
            {"416", "Range Not Satisfiable"},
            {"417", "Expectation Failed"},
            {"418", "I\"m a teapot"},
            {"421", "Misdirected Request"},
            {"422", "Unprocessable Entity"},
            {"423", "Locked"},
            {"424", "Failed Dependency"},
            {"426", "Upgrade Required"},
            {"428", "Precondition Required"},
            {"429", "Too Many Requests"},
            {"431", "Request Header Fields Too Large"},
            {"451", "Unavailable For Legal Reasons"},
            {"500", "Internal Server Error"},
            {"501", "Not Implemented"},
            {"502", "Bad Gateway"},
            {"503", "Service Unavailable"},
            {"504", "Gateway Timeout"},
            {"505", "HTTP Version Not Supported"},
            {"506", "Variant Also Negotiates"},
            {"507", "Insufficient Storage"},
            {"508", "Loop Detected"},
            {"510", "Not Extended"},
            {"511", "Network Authentication Required"}
        };

        private readonly CDPSession _session;
        private readonly JToken _params;

        private bool _done;
        private Queue<RequestHandlerAsync> _handlers;

        public HttpRequest(CDPSession session, JToken @params, Queue<RequestHandlerAsync> handlers)
        {
            _session = session;
            _params = @params;
            _handlers = handlers;
        }

        public string Url 
        { 
            get
            {
                return _params["request"]["url"].Value<string>();
            } 
        }

        public string ResourceType 
        { 
            get 
            {
                return _params["resourceType"].Value<string>();
            } 
        }

        public async Task CallNextHandlerAsync()
        {
            DebugServer("next handler {0}", Url);

            if (_handlers.Count > 0) 
            {
                var handler = _handlers.Dequeue();

                await handler(this);

                return;
            }

            await ResolveAsync(JObject.Parse("{}"));
        }

        private void DebugServer(string message, params string[] args)
        {
            Console.WriteLine(message, args);
        }

        internal async Task<JObject> FulfillAsync(int? status, Dictionary<string, string> headers, byte[] body) 
        {
            DebugServer("fulfill {0}", Url);

            status = status ?? 200;

            var responseHeaders = new Dictionary<string, string>();

            if (headers != null) 
            {
                foreach (var header in headers.Keys)
                {
                    responseHeaders[header.ToLower()] = headers[header];
                }
            }
            
            if (body != null && !(responseHeaders.ContainsKey("content-length")))
            {
                responseHeaders["content-length"] = body.Length.ToString();
            }

            var statusText = _statusTexts[status.ToString()] ?? "";
            var statusLine = $"HTTP/1.1 {status} {statusText}";

            var CRLF = "\r\n";
            var text = statusLine + CRLF;

            foreach (var header in responseHeaders.Keys)
            {
                text += header + ": " + responseHeaders[header] + CRLF;
            }

            text += CRLF;

            var headerInBytes = System.Text.Encoding.UTF8.GetBytes(text);

            var responseBuffer = new MemoryStream();

            responseBuffer.Write(headerInBytes, 0, headerInBytes.Length);

            if (body != null)
            {
                responseBuffer.Write(body, 0, body.Length);
            }

            var content = Convert.ToBase64String(responseBuffer.ToArray());
             
            var @params = new JObject
            {
                { "interceptionId", null },
                { "rawResponse", content }
            };

            return await ResolveAsync(@params);
        }

        internal async Task<JObject> DeferToBrowserAsync(JObject overrides = null)
        {
            DebugServer("deferToBrowser  {0}", Url);
            
            var @params = JObject.Parse("{}");

            if (overrides != null && overrides["url"] != null) 
            {
                @params["url"] = overrides["url"];
            }

            if (overrides != null && overrides["method"] != null) 
            {
                @params["method"] = overrides["method"];
            }

            if (overrides != null && overrides["headers"] != null) 
            {
                @params["headers"] = overrides["headers"];
            }
            
            return await ResolveAsync(@params);
        }

        private async Task<JObject> ResolveAsync(JObject @params)
        {
            DebugServer("resolve  {0}", Url);

            if (_done) 
            {
                throw new Exception("Already resolved given request");
            }

            @params["interceptionId"] = _params["interceptionId"];

            _done = true;
            
            return await _session.SendAsync("Network.continueInterceptedRequest", @params);
        }
    }
}