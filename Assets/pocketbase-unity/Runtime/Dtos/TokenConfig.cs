using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of a single collection token config.
    /// </summary>
    [Serializable]
    public class TokenConfig
    {
        [JsonProperty("duration")]
        public float Duration { get; set; }

        [JsonProperty("secret")]
        public string Secret { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}