using System;
using System.Collections.Generic;
using System.Text;

namespace CarloSharp
{
    public class IpcMessageEventArgs : EventArgs
    {
        internal IpcMessageEventArgs(string channel, string message)
        {
            Channel = channel;
            Message = message;
        }

        public string Channel { get; private set; }

        public string Message { get; private set; }
    }
}
