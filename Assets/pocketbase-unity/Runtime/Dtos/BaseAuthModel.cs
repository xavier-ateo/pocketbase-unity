using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    [Serializable]
    public class BaseAuthModel : RecordModel
    {
        [JsonProperty("email")]
        public string Email { get; private set; }

        [JsonProperty("emailVisibility")]
        public bool? EmailVisibility { get; private set; }

        [JsonProperty("name")]
        public string Username { get; private set; }

        [JsonProperty("verified")]
        public bool? Verified { get; set; }
    }
}