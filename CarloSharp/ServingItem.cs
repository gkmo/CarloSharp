using System.Reflection;

namespace CarloSharp 
{
    internal class ServingItem
    {
        public string Prefix { get; set; }

        public string Folder { get; set; }

        public string BaseUrl { get; set; }

        public Assembly Assembly { get; set; }

        public string DefaultNamespace { get; set; }
    }   
}