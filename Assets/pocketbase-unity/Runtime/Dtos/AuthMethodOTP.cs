using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of otp auth method option.
    /// </summary>
    [Serializable]
    public class AuthMethodOTP
    {
        [JsonProperty("duration")]
        public float Duration { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
        
        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}