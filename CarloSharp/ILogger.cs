using System;
using System.Collections.Generic;
using System.Text;

namespace CarloSharp
{
    public interface ILogger
    {
        void Debug(string message, params string[] args);

        void Error(string message, params string[] args);
    }
}
