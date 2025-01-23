using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PocketBaseSdk
{
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

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public JToken this[string key] => _additionalData.TryGetValue(key, out var value) ? value : null;

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}