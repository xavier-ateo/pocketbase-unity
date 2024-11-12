using System;
using Newtonsoft.Json;

[Serializable]
public class RecordModel
{
    [JsonProperty("id")]
    public string Id { get; private set; }

    [JsonProperty("collectionId")]
    public string CollectionId { get; private set; }

    [JsonProperty("collectionName")]
    public string CollectionName { get; private set; }

    [JsonProperty("created")]
    public DateTime? Created { get; private set; }

    [JsonProperty("updated")]
    public DateTime? Updated { get; private set; }
}