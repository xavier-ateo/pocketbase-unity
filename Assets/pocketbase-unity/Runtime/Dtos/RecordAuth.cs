using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of the record authentication data.
    /// </summary>
    [Serializable]
    public class RecordAuth
    {
        public RecordAuth(string token, RecordModel record)
        {
            Token = token;
            Record = record;
        }

        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("record")]
        public RecordModel Record { get; private set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}