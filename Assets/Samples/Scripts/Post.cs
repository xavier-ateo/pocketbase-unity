using Newtonsoft.Json;
using PocketBaseSdk;

public class Post : RecordModel
{
    [JsonProperty("content")]
    public string Content { get; set; }
}