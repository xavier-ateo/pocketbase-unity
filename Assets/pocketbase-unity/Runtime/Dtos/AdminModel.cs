using System;
using Newtonsoft.Json;

[Serializable]
public class AdminModel : RecordModel
{
    [JsonProperty("email")]
    public string Email { get; private set; }

    [JsonProperty("avatar")]
    public int Avatar { get; private set; }
}