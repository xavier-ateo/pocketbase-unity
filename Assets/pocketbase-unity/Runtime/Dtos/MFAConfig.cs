using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of a single collection mfa auth config.
    /// </summary>
    [Serializable]
    public class MFAConfig
    {
        [JsonProperty("Duration")]
        public float Duration { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("rule")]
        public string Rule { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}