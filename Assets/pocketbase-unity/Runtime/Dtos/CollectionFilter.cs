using System.Collections.Generic;

namespace PocketBaseSdk
{
    public class CollectionFilter
    {
        public string Expands { get; set; } = string.Empty;
        public string Fields { get; set; } = string.Empty;
        public Dictionary<string, object> Query { get; } = new();
        public Dictionary<string, string> Headers { get; } = new();
    }
}