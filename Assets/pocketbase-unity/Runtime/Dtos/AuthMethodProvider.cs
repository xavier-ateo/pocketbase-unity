using System;
using Newtonsoft.Json;

[Serializable]
public class AuthMethodProvider
{
    [JsonProperty("name")]
    public string Name { get; private set; } = string.Empty;

    [JsonProperty("displayName")]
    public string DisplayName { get; private set; } = string.Empty;

    [JsonProperty("state")]
    public string State { get; private set; } = string.Empty;

    [JsonProperty("codeVerifier")]
    public string CodeVerifier { get; private set; } = string.Empty;

    [JsonProperty("codeChallenge")]
    public string CodeChallenge { get; private set; } = string.Empty;

    [JsonProperty("codeChallengeMethod")]
    public string CodeChallengeMethod { get; private set; } = string.Empty;

    [JsonProperty("authUrl")]
    public string AuthUrl { get; private set; } = string.Empty;
}