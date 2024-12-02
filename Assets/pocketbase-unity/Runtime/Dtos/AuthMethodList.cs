using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class AuthMethodList
{
    [JsonProperty("usernamePassword")]
    public bool UsernamePassword { get; private set; }

    [JsonProperty("emailPassword")]
    public bool EmailPassword { get; private set; }

    [JsonProperty("onlyVerified")]
    public bool OnlyVerified { get; private set; }

    [JsonProperty("authProviders")]
    public List<AuthMethodProvider> AuthProviders { get; private set; }
}