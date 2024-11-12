using Newtonsoft.Json;

public class Post : RecordModel
{
    [JsonProperty("content")]
    public string Content { get; set; }
}