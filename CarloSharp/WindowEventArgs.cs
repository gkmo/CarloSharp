using System;

namespace CarloSharp
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
