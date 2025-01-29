using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of a single collection auth alert config.
    /// </summary>
    [Serializable]
    public class AuthAlertConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("emailTemplate")]
        public EmailTemplateConfig EmailTemplate { get; set; } = new();
        
        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}