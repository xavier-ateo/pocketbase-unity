using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    [Serializable]

    public class AppleClientSecret
    {
        [JsonProperty("secret")]
        string Secret { get; set; }
    }
}