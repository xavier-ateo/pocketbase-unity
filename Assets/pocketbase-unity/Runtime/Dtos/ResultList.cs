using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    [Serializable]

    public class ResultList<T>
    {
        [JsonProperty("page")]
        public int Page { get; private set; }

        [JsonProperty("perPage")]
        public int PerPage { get; private set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; private set; }

        [JsonProperty("totalItems")]
        public int TotalItems { get; private set; }

        [JsonProperty("items")]
        public List<T> Items { get; private set; }
    }
}