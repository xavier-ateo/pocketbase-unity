using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public sealed class CollectionModel
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = "base";

    [JsonProperty("created")]
    public string Created { get; set; } = string.Empty;

    [JsonProperty("updated")]
    public string Updated { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("system")]
    public bool System { get; set; }

    [JsonProperty("listRule")]
    public string ListRule { get; set; }

    [JsonProperty("viewRule")]
    public string ViewRule { get; set; }

    [JsonProperty("createRule")]
    public string CreateRule { get; set; }

    [JsonProperty("updateRule")]
    public string UpdateRule { get; set; }

    [JsonProperty("deleteRule")]
    public string DeleteRule { get; set; }

    [JsonProperty("schema")]
    public List<SchemaField> Schema { get; set; } = new();

    [JsonProperty("indexes")]
    public List<string> Indexes { get; set; } = new();

    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; } = new();
}