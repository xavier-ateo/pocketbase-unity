using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of a backup file info entry.
    /// </summary>
    [Serializable]
    public class BackupFileInfo
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("modified")]
        public string Modified { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}