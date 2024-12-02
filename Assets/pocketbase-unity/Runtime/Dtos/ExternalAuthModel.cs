using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    [Serializable]

    public class ExternalAuthModel
    {
        [JsonProperty("id")]
        public string Id { get; private set; } = string.Empty;

        [JsonProperty("created")]
        public string Created { get; private set; } = string.Empty;

        [JsonProperty("updated")]
        public string Updated { get; private set; } = string.Empty;

        [JsonProperty("recordId")]
        public string RecordId { get; private set; } = string.Empty;

        [JsonProperty("collectionId")]
        public string CollectionId { get; private set; } = string.Empty;

        [JsonProperty("provider")]
        public string Provider { get; private set; } = string.Empty;

        [JsonProperty("providerId")]
        public string ProviderId { get; private set; } = string.Empty;
    }
}