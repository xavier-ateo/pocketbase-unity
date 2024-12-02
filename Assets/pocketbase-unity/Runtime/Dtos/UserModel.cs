using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    [Serializable]
    public class UserModel : BaseAuthModel
    {
        [JsonProperty("avatar")]
        public string Avatar { get; private set; }
    }
}