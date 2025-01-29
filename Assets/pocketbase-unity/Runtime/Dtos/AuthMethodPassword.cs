using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of password/identity auth method option.
    /// </summary>
    [Serializable]
    public class AuthMethodPassword
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("identityFields")]
        public List<string> IdentityFields { get; set; } = new();
        
        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}