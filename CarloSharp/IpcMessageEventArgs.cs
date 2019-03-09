using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace CarloSharp
{
    public class IpcMessageEventArgs : EventArgs
    {
        internal IpcMessageEventArgs(string channel, JToken message)
        {
            Channel = channel;
            Message = message;
        }

        public string Channel { get; private set; }

        public JToken Message { get; private set; }

        public object Result { get; set; }
    }
}
