using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    [Serializable]
    public class AuthMethodList
    {
        [JsonProperty("mfa")]
        public AuthMethodMFA MFA { get; private set; } = new();

        [JsonProperty("otp")]
        public AuthMethodOTP OTP { get; private set; } = new();

        [JsonProperty("password")]
        public AuthMethodPassword Password { get; private set; } = new();

        [JsonProperty("oauth2")]
        public AuthMethodOAuth2 OAuth2 { get; private set; } = new();
    }
}