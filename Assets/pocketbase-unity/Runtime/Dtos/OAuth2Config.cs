using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of a single collection oauth2 auth config.
    /// </summary>
    [Serializable]
    public class OAuth2Config
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("mappedFields")]
        public Dictionary<string, string> MappedFields { get; set; } = new();

        [JsonProperty("providers")]
        public List<object> Providers { get; set; } = new();

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}