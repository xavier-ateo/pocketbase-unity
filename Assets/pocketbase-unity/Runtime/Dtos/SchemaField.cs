using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    [Serializable]

    public sealed class SchemaField
    {
        [JsonProperty("id")]
        string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        string Name { get; set; } = string.Empty;

        [JsonProperty("type")]
        string Type { get; set; } = string.Empty;

        [JsonProperty("system")]
        public bool System { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }

        [JsonProperty("presentable")]
        public bool Presentable { get; set; }

        [JsonProperty("options")]
        public Dictionary<string, object> Options { get; set; } = new();
    }
}