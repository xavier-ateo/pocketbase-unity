using System;
using Newtonsoft.Json;

[Serializable]
public class AppleClientSecret
{
    [JsonProperty("secret")]
    string Secret { get; set; }
}