using Newtonsoft.Json;
using PocketBaseSdk;

public class Post : RecordModel
{
    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; }

    public static Post FromRecord(RecordModel record) => JsonConvert.DeserializeObject<Post>(record.ToString());
}