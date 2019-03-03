using System;
using System.Collections.Generic;
using System.Text;

namespace CarloSharp
{
    public class ConsoleLogger : ILogger
    {
        public void Debug(string message, params string[] args)
        {
            Console.WriteLine("DEBUG:" + message, args);
        }

        public void Error(string message, params string[] args)
        {
            Console.WriteLine("ERROR: " + message, args);
        }
    }
}
