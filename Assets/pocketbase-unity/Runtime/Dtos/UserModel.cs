using System;
using Newtonsoft.Json;

[Serializable]
public class UserModel : BaseAuthModel
{
    [JsonProperty("avatar")]
    public string Avatar { get; private set; }
}