using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of a single collection password auth config.
    /// </summary>
    [Serializable]
    public class PasswordAuthConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("identityFields")]
        public List<string> IdentityFields { get; set; } = new();

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}