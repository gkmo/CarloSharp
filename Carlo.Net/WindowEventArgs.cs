using System;
using System.Collections.Generic;
using System.Text;

namespace Carlo.Net
{
    public class WindowEventArgs : EventArgs
    {
        public WindowEventArgs(Window window)
        {
            Window = window;
        }

        public Window Window { get; private set; }
    }
}
