using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PocketBaseSdk
{
    [Serializable]
    public class RecordModel : JObject
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

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}