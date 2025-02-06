using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    [Serializable]
    public class AuthMethodOAuth2
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("providers")]
        public List<AuthMethodProvider> Providers { get; set; } = new();

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}