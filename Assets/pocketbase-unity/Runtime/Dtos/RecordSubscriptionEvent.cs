using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    [Serializable]

    public class RecordSubscriptionEvent
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("record")]
        public RecordModel Record { get; set; }
    }
}