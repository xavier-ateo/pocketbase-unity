using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of a single collection email template config.
    /// </summary>
    [Serializable]
    public class EmailTemplateConfig
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}