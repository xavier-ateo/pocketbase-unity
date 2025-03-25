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
        public string Id { get; protected set; }

        [JsonProperty("collectionId")]
        public string CollectionId { get; protected set; }

        [JsonProperty("collectionName")]
        public string CollectionName { get; protected set; }

        [JsonProperty("created")]
        public DateTime? Created { get; protected set; }

        [JsonProperty("updated")]
        public DateTime? Updated { get; protected set; }

        [JsonExtensionData]
        protected IDictionary<string, JToken> _additionalData;

        [JsonIgnore]
        public Dictionary<string, dynamic> Data =>
            JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(ToString());

        [JsonIgnore]
        public ICollection<string> AdditionalDataKeys => _additionalData.Keys;
        
        public RecordModel()
        {
            _additionalData = new Dictionary<string, JToken>();
        }

        public static RecordModel Create(Dictionary<string, dynamic> data)
        {
            var json = JsonConvert.SerializeObject(data);
            return JsonConvert.DeserializeObject<RecordModel>(json);
        }

        public JToken this[string key]
        {
            get => _additionalData.TryGetValue(key, out var value) ? value : null;
            set => _additionalData[key] = value;
        }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}