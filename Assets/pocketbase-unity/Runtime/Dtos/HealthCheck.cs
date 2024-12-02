using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class HealthCheck
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public Dictionary<string, object> Data { get; set; } = new();
}